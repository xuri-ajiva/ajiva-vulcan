using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ajiva.Engine;
using ajiva.Models;
using SharpVk;

namespace ajiva.EngineManagers
{
    public class GraphicsManager : IEngineManager
    {
        private readonly IEngine engine;

        public PipelineLayout PipelineLayout { get; private set; }
        public RenderPass RenderPass { get; private set; }
        public Pipeline Pipeline { get; private set; }

        public DescriptorPool DescriptorPool { get; private set; }
        public DescriptorSetLayout DescriptorSetLayout { get; private set; }
        public DescriptorSet DescriptorSet { get; private set; }

        public GraphicsManager(IEngine engine)
        {
            this.engine = engine;
        }

        public void CreateGraphicsPipeline()
        {
            var bindingDescription = Vertex.GetBindingDescription();
            var attributeDescriptions = Vertex.GetAttributeDescriptions();

            PipelineLayout = engine.DeviceManager.Device.CreatePipelineLayout(engine.GraphicsManager.DescriptorSetLayout, null);

            Pipeline = engine.DeviceManager.Device.CreateGraphicsPipelines(null, new[]
            {
                new GraphicsPipelineCreateInfo
                {
                    Layout = PipelineLayout,
                    RenderPass = RenderPass,
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
                                X = 0f,
                                Y = 0f,
                                Width = engine.SwapChainManager.SwapChainExtent.Width,
                                Height = engine.SwapChainManager.SwapChainExtent.Height,
                                MaxDepth = 1,
                                MinDepth = 0,
                            }
                        },
                        Scissors = new[]
                        {
                            new Rect2D
                            {
                                Offset = new Offset2D(),
                                Extent = engine.SwapChainManager.SwapChainExtent
                            }
                        }
                    },
                    RasterizationState = new()
                    {
                        DepthClampEnable = false,
                        RasterizerDiscardEnable = false,
                        PolygonMode = PolygonMode.Fill,
                        LineWidth = 1,
                        //CullMode = CullModeFlags.Back,
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
                            Module = engine.ShaderManager.VertShader,
                            Name = "main"
                        },
                        new PipelineShaderStageCreateInfo
                        {
                            Stage = ShaderStageFlags.Fragment,
                            Module = engine.ShaderManager.FragShader,
                            Name = "main"
                        }
                    },
                    DepthStencilState = new()
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
                }
            }).Single();
        }

        public void CreateRenderPass()
        {
            RenderPass = engine.DeviceManager.Device.CreateRenderPass(
                new AttachmentDescription[]
                {
                    new()
                    {
                        Format = engine.SwapChainManager.SwapChainFormat,
                        Samples = SampleCountFlags.SampleCount1,
                        LoadOp = AttachmentLoadOp.Clear,
                        StoreOp = AttachmentStoreOp.Store,
                        StencilLoadOp = AttachmentLoadOp.DontCare,
                        StencilStoreOp = AttachmentStoreOp.DontCare,
                        InitialLayout = ImageLayout.Undefined,
                        FinalLayout = ImageLayout.PresentSource
                    },
                    new()
                    {
                        Format = engine.ImageManager.FindDepthFormat(),
                        Samples = SampleCountFlags.SampleCount1,
                        LoadOp = AttachmentLoadOp.Clear,
                        StoreOp = AttachmentStoreOp.DontCare,
                        StencilLoadOp = AttachmentLoadOp.DontCare,
                        StencilStoreOp = AttachmentStoreOp.DontCare,
                        InitialLayout = ImageLayout.Undefined,
                        FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
                    }
                },
                new SubpassDescription
                {
                    DepthStencilAttachment = new(1, ImageLayout.DepthStencilAttachmentOptimal),
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
                        DestinationAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite | AccessFlags.DepthStencilAttachmentRead
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
        }

        public void CreateDescriptorSetLayout()
        {
            DescriptorSetLayout = engine.DeviceManager.Device.CreateDescriptorSetLayout(
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
                        DescriptorCount = 1,
                        DescriptorType = DescriptorType.CombinedImageSampler,
                        StageFlags = ShaderStageFlags.Fragment,
                    }
                });
        }

        public void CreateDescriptorPool()
        {
            DescriptorPool = engine.DeviceManager.Device.CreateDescriptorPool(
                1,
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
                        Type = DescriptorType.CombinedImageSampler
                    }
                });
        }

        public void CreateDescriptorSet()
        {
            DescriptorSet = engine.DeviceManager.Device.AllocateDescriptorSets(DescriptorPool, DescriptorSetLayout).Single();

            engine.DeviceManager.Device.UpdateDescriptorSets(
                new WriteDescriptorSet[]
                {
                    new()
                    {
                        BufferInfo = new[]
                        {
                            new DescriptorBufferInfo
                            {
                                Buffer = engine.BufferManager.UniformBuffer,
                                Offset = 0,
                                Range = (ulong)Unsafe.SizeOf<UniformBufferObject>()
                            }
                        },
                        DescriptorCount = 1,
                        DestinationSet = DescriptorSet,
                        DestinationBinding = 0,
                        DestinationArrayElement = 0,
                        DescriptorType = DescriptorType.UniformBuffer
                    },
                    new()
                    {
                        ImageInfo = new[]
                        {
                            new DescriptorImageInfo // todo make modular
                            {
                                Sampler = engine.TextureManager.Logo.Sampler,
                                ImageView = engine.TextureManager.Logo.Image.View,

                                //Sampler = engine.textureSampler,
                                //ImageView = engine.textureImageView,
                                ImageLayout = ImageLayout.ShaderReadOnlyOptimal
                            }
                        },
                        DescriptorCount = 1,
                        DestinationSet = DescriptorSet,
                        DestinationBinding = 1,
                        DestinationArrayElement = 0,
                        DescriptorType = DescriptorType.CombinedImageSampler,
                    }
                }, null);
        }
    }
}
