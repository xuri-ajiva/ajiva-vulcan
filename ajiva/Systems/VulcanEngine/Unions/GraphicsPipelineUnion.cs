using System.Collections.Generic;
using System.Linq;
using ajiva.Components;
using ajiva.Models;
using ajiva.Models.Buffer;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Unions
{
    public class GraphicsPipelineUnion : DisposingLogger
    {
        public RenderPass RenderPass { get; init; }
        public Pipeline Pipeline { get; init; }
        public PipelineLayout PipelineLayout { get; init; }
        public DescriptorPool DescriptorPool { get; init; }
        public DescriptorSet DescriptorSet { get; init; }
        public DescriptorSetLayout DescriptorSetLayout { get; init; }

        public GraphicsPipelineUnion(RenderPass renderPass, Pipeline pipeline, PipelineLayout pipelineLayout, DescriptorPool descriptorPool, DescriptorSet descriptorSet, DescriptorSetLayout descriptorSetLayout)
        {
            RenderPass = renderPass;
            Pipeline = pipeline;
            PipelineLayout = pipelineLayout;
            DescriptorPool = descriptorPool;
            DescriptorSet = descriptorSet;
            DescriptorSetLayout = descriptorSetLayout;
        }

        public static GraphicsPipelineUnion CreateGraphicsPipelineUnion3D(SwapChainUnion swapChainRecord, PhysicalDevice physicalDevice, Device device, ShaderSystem system, DescriptorImageInfo[] textureSamplerImageViews, Canvas canvas)
        {
            return CreateGraphicsPipelineUnion(swapChainRecord, physicalDevice, device, true, Vertex3D.GetBindingDescription(), Vertex3D.GetAttributeDescriptions(), system.ShaderUnions[AjivaEngineLayer.Layer3d].Main, system.ShaderUnions[AjivaEngineLayer.Layer3d].ViewProj, system.ShaderUnions[AjivaEngineLayer.Layer3d].UniformModels, textureSamplerImageViews,  canvas);
        }

        public static GraphicsPipelineUnion CreateGraphicsPipelineUnion2D(SwapChainUnion swapChainRecord, PhysicalDevice physicalDevice, Device device, ShaderSystem system, DescriptorImageInfo[] textureSamplerImageViews, Canvas canvas)
        {
            return CreateGraphicsPipelineUnion(swapChainRecord, physicalDevice, device, false, Vertex2D.GetBindingDescription(), Vertex2D.GetAttributeDescriptions(), system.ShaderUnions[AjivaEngineLayer.Layer2d].Main, system.ShaderUnions[AjivaEngineLayer.Layer2d].ViewProj, system.ShaderUnions[AjivaEngineLayer.Layer2d].UniformModels, textureSamplerImageViews,  canvas);
        }

        public static GraphicsPipelineUnion CreateGraphicsPipelineUnion(SwapChainUnion swapChainRecord, PhysicalDevice physicalDevice, Device device, bool useDepthImage, VertexInputBindingDescription bindingDescription, VertexInputAttributeDescription[] attributeDescriptions, Shader mainShader, UniformBuffer<UniformViewProj> viewProj, UniformBuffer<UniformModel> uniformModels, DescriptorImageInfo[] textureSamplerImageViews, Canvas canvas)
        {
            var attach = new List<AttachmentDescription>();
            attach.Add(new(AttachmentDescriptionFlags.None, swapChainRecord.SwapChainFormat, SampleCountFlags.SampleCount1, useDepthImage ? AttachmentLoadOp.Clear : AttachmentLoadOp.DontCare, AttachmentStoreOp.Store, AttachmentLoadOp.DontCare, AttachmentStoreOp.DontCare, ImageLayout.Undefined, ImageLayout.PresentSource));
            if (useDepthImage) attach.Add(new(AttachmentDescriptionFlags.None, physicalDevice.FindDepthFormat(), SampleCountFlags.SampleCount1, AttachmentLoadOp.Clear, AttachmentStoreOp.DontCare, AttachmentLoadOp.DontCare, AttachmentStoreOp.DontCare, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal));

            RenderPass renderPass = device!.CreateRenderPass(attach.ToArray(),
                new SubpassDescription
                {
                    DepthStencilAttachment = useDepthImage ? new(1, ImageLayout.DepthStencilAttachmentOptimal) : null,
                    PipelineBindPoint = PipelineBindPoint.Graphics,
                    ColorAttachments = new[]
                    {
                        new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal)
                    }
                },
                new[]
                {
                    new SubpassDependency
                    {
                        SourceSubpass = Constants.SubpassExternal,
                        DestinationSubpass = 0,
                        SourceStageMask = PipelineStageFlags.BottomOfPipe,
                        SourceAccessMask = AccessFlags.MemoryRead,
                        DestinationStageMask = PipelineStageFlags.ColorAttachmentOutput | PipelineStageFlags.EarlyFragmentTests,
                        DestinationAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite | AccessFlags.DepthStencilAttachmentRead //todo remove some if no depth image
                    },
                    new SubpassDependency
                    {
                        SourceSubpass = 0,
                        DestinationSubpass = Constants.SubpassExternal,
                        SourceStageMask = PipelineStageFlags.ColorAttachmentOutput | PipelineStageFlags.EarlyFragmentTests,
                        SourceAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite | AccessFlags.DepthStencilAttachmentRead,
                        DestinationStageMask = PipelineStageFlags.BottomOfPipe,
                        DestinationAccessMask = AccessFlags.MemoryRead
                    },
                });

            DescriptorSetLayout descriptorSetLayout = device.CreateDescriptorSetLayout(
                new DescriptorSetLayoutBinding[]
                {
                    new()
                    {
                        Binding = 0,
                        DescriptorType = DescriptorType.UniformBuffer,
                        StageFlags = ShaderStageFlags.Vertex,
                        DescriptorCount = 1
                    },
                    new()
                    {
                        Binding = 1,
                        DescriptorType = DescriptorType.UniformBufferDynamic,
                        StageFlags = ShaderStageFlags.Vertex,
                        DescriptorCount = 1
                    },
                    new()
                    {
                        Binding = 2,
                        DescriptorCount = TextureSystem.MAX_TEXTURE_SAMPLERS_IN_SHADER,
                        DescriptorType = DescriptorType.CombinedImageSampler,
                        StageFlags = ShaderStageFlags.Fragment,
                    }
                });

            PipelineLayout pipelineLayout = device.CreatePipelineLayout(descriptorSetLayout, null);

            Pipeline pipeline = device.CreateGraphicsPipelines(null, new[]
            {
                new GraphicsPipelineCreateInfo
                {
                    Layout = pipelineLayout,
                    RenderPass = renderPass,
                    Subpass = 0,
                    VertexInputState = new()
                    {
                        VertexBindingDescriptions = new[]
                        {
                            bindingDescription
                        },
                        VertexAttributeDescriptions = attributeDescriptions
                    },
                    InputAssemblyState = new()
                    {
                        PrimitiveRestartEnable = false,
                        Topology = PrimitiveTopology.TriangleList
                    },
                    ViewportState = new()
                    {
                        Viewports = new[]
                        {
                            new Viewport
                            {
                                X = canvas.Xf,
                                Y = canvas.Yf,
                                Width = canvas.WidthF,
                                Height = canvas.HeightF,
                                MaxDepth = 1,
                                MinDepth = 0,
                            }
                        },
                        Scissors = new[]
                        {
                            new Rect2D
                            {
                                Offset = canvas.Offset,
                                Extent = canvas.Extent
                            }
                        }
                    },
                    RasterizationState = new()
                    {
                        DepthClampEnable = false,
                        RasterizerDiscardEnable = false,
                        PolygonMode = PolygonMode.Fill,
                        LineWidth = 1,
                        //CullMode = CullModeFlags.Back,          // rnable to make faces only visible from one side
                        //FrontFace = FrontFace.CounterClockwise,
                        DepthBiasEnable = false
                    },
                    MultisampleState = new()
                    {
                        SampleShadingEnable = false,
                        RasterizationSamples = SampleCountFlags.SampleCount1,
                        MinSampleShading = 1
                    },
                    ColorBlendState = new()
                    {
                        Attachments = new[]
                        {
                            new PipelineColorBlendAttachmentState
                            {
                                ColorWriteMask = ColorComponentFlags.R
                                                 | ColorComponentFlags.G
                                                 | ColorComponentFlags.B
                                                 | ColorComponentFlags.A,
                                BlendEnable = false,
                                SourceColorBlendFactor = BlendFactor.One,
                                DestinationColorBlendFactor = BlendFactor.Zero,
                                ColorBlendOp = BlendOp.Add,
                                SourceAlphaBlendFactor = BlendFactor.One,
                                DestinationAlphaBlendFactor = BlendFactor.Zero,
                                AlphaBlendOp = BlendOp.Add
                            }
                        },
                        LogicOpEnable = false,
                        LogicOp = LogicOp.Copy,
                        BlendConstants = (0, 0, 0, 0)
                    },
                    Stages = new[]
                    {
                        new PipelineShaderStageCreateInfo
                        {
                            Stage = ShaderStageFlags.Vertex,
                            Module = mainShader.VertShader,
                            Name = "main"
                        },
                        new PipelineShaderStageCreateInfo
                        {
                            Stage = ShaderStageFlags.Fragment,
                            Module = mainShader.FragShader,
                            Name = "main"
                        }
                    },
                    DepthStencilState = useDepthImage
                        ? new()
                        {
                            DepthTestEnable = true,
                            DepthWriteEnable = true,
                            DepthCompareOp = CompareOp.Less,
                            DepthBoundsTestEnable = false,
                            MinDepthBounds = 0,
                            MaxDepthBounds = 1,
                            StencilTestEnable = false,
                            Back = new(),
                            Flags = new(),
                        }
                        : null,
                }
            }).Single();

            DescriptorPool descriptorPool = device.CreateDescriptorPool(10000,
                new DescriptorPoolSize[]
                {
                    new()
                    {
                        DescriptorCount = 1,
                        Type = DescriptorType.UniformBuffer
                    },
                    new()
                    {
                        DescriptorCount = 1,
                        Type = DescriptorType.UniformBufferDynamic
                    },
                    new()
                    {
                        DescriptorCount = (uint)textureSamplerImageViews.Length,
                        Type = DescriptorType.CombinedImageSampler
                    }
                });
            DescriptorSet descriptorSet = device!.AllocateDescriptorSets(descriptorPool, descriptorSetLayout).Single();

            device.UpdateDescriptorSets(
                new WriteDescriptorSet[]
                {
                    new()
                    {
                        BufferInfo = new[]
                        {
                            new DescriptorBufferInfo
                            {
                                Buffer = viewProj.Uniform.Buffer,
                                Offset = 0,
                                Range = viewProj.Uniform.SizeOfT
                            }
                        },
                        DescriptorCount = 1,
                        DestinationSet = descriptorSet,
                        DestinationBinding = 0,
                        DestinationArrayElement = 0,
                        DescriptorType = DescriptorType.UniformBuffer
                    },
                    new()
                    {
                        BufferInfo = new[]
                        {
                            new DescriptorBufferInfo
                            {
                                Buffer = uniformModels.Uniform.Buffer,
                                Offset = 0,
                                Range = uniformModels.Uniform.SizeOfT
                            }
                        },
                        DescriptorCount = 1,
                        DestinationSet = descriptorSet,
                        DestinationBinding = 1,
                        DestinationArrayElement = 0,
                        DescriptorType = DescriptorType.UniformBufferDynamic
                    },
                    new()
                    {
                        ImageInfo = textureSamplerImageViews,
                        DescriptorCount = (uint)textureSamplerImageViews.Length,
                        DestinationSet = descriptorSet,
                        DestinationBinding = 2,
                        DestinationArrayElement = 0,
                        DescriptorType = DescriptorType.CombinedImageSampler,
                    }
                }, null);

            return new(renderPass, pipeline, pipelineLayout, descriptorPool, descriptorSet, descriptorSetLayout);
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            RenderPass.Dispose();
            PipelineLayout.Dispose();
            Pipeline.Dispose();
            DescriptorSetLayout.Dispose();
            DescriptorPool.Dispose();
        }
    }
}
