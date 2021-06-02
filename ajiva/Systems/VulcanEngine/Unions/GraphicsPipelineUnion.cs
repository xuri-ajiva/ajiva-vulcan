using System.Collections.Generic;
using System.Linq;
using ajiva.Components;
using ajiva.Models;
using ajiva.Systems.VulcanEngine.Layer;
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
        
        public static GraphicsPipelineUnion CreateGraphicsPipelineUnion(SwapChainUnion swapChainRecord, PhysicalDevice physicalDevice, Device device, bool useDepthImage, VertexInputBindingDescription bindingDescription, VertexInputAttributeDescription[] attributeDescriptions, Shader mainShader, PipelineDescriptorInfos[] descriptorInfos, Canvas canvas)
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
                        DestinationAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite | (useDepthImage ? AccessFlags.DepthStencilAttachmentRead : 0)
                    },
                    new SubpassDependency
                    {
                        SourceSubpass = 0,
                        DestinationSubpass = Constants.SubpassExternal,
                        SourceStageMask = PipelineStageFlags.ColorAttachmentOutput | PipelineStageFlags.EarlyFragmentTests,
                        SourceAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite | (useDepthImage ? AccessFlags.DepthStencilAttachmentRead : 0),
                        DestinationStageMask = PipelineStageFlags.BottomOfPipe,
                        DestinationAccessMask = AccessFlags.MemoryRead
                    },
                });

            DescriptorSetLayout descriptorSetLayout = device.CreateDescriptorSetLayout(
                descriptorInfos.Select(descriptor => new DescriptorSetLayoutBinding()
                {
                    Binding = descriptor.DestinationBinding,
                    DescriptorCount = descriptor.DescriptorCount,
                    DescriptorType = descriptor.DescriptorType,
                    StageFlags = descriptor.StageFlags
                }).ToArray());

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
                        //CullMode = CullModeFlags.Back,          // reenable to make faces only visible from one side
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
                        mainShader.VertShaderPipelineStageCreateInfo,
                        mainShader.FragShaderPipelineStageCreateInfo,
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
                descriptorInfos.Select(descriptor => new DescriptorPoolSize
                {
                    Type = descriptor.DescriptorType,
                    DescriptorCount = descriptor.DescriptorCount,
                }).ToArray());
            DescriptorSet descriptorSet = device!.AllocateDescriptorSets(descriptorPool, descriptorSetLayout).Single();

            device.UpdateDescriptorSets(
                descriptorInfos.Select(descriptor => new WriteDescriptorSet
                {
                    DestinationSet = descriptorSet,
                    DescriptorCount = descriptor.DescriptorCount,
                    DestinationBinding = descriptor.DestinationBinding,
                    DescriptorType = descriptor.DescriptorType,
                    DestinationArrayElement = descriptor.DestinationArrayElement,
                    BufferInfo = descriptor.BufferInfo,
                    ImageInfo = descriptor.ImageInfo,
                    TexelBufferView = descriptor.TexelBufferView,
                }).ToArray(), null);

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
