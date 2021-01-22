using System.Linq;
using ajiva.Models;
using ajiva.Systems.RenderEngine.Engine;
using SharpVk;

namespace ajiva.Systems.RenderEngine.EngineManagers
{
    public class GraphicsLayout : RenderEngineComponent, IThreadSaveCreatable
    {
        public PipelineLayout? PipelineLayout { get; private set; }
        public RenderPass? RenderPass { get; private set; }
        public Pipeline? Pipeline { get; private set; }

        public DescriptorPool? DescriptorPool { get; private set; }
        public DescriptorSetLayout? DescriptorSetLayout { get; private set; }
        public DescriptorSet? DescriptorSet { get; private set; }

        public GraphicsLayout(IRenderEngine renderEngine) : base(renderEngine)
        {
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            PipelineLayout?.Dispose();
            RenderPass?.Dispose();
            Pipeline?.Dispose();
            DescriptorPool?.Dispose();
            DescriptorSetLayout?.Dispose();
        }

        public void CreateGraphicsPipeline()
        {
            var bindingDescription = Vertex.GetBindingDescription();
            var attributeDescriptions = Vertex.GetAttributeDescriptions();

            PipelineLayout = RenderEngine.DeviceComponent.Device!.CreatePipelineLayout(DescriptorSetLayout, null);

            Pipeline = RenderEngine.DeviceComponent.Device.CreateGraphicsPipelines(null, new[]
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
                                Width = RenderEngine.SwapChainComponent.SwapChainExtent!.Value.Width,
                                Height = RenderEngine.SwapChainComponent.SwapChainExtent!.Value.Height,
                                MaxDepth = 1,
                                MinDepth = 0,
                            }
                        },
                        Scissors = new[]
                        {
                            new Rect2D
                            {
                                Offset = Offset2D.Zero,
                                Extent = RenderEngine.SwapChainComponent.SwapChainExtent!.Value
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
                            Module = RenderEngine.ShaderComponent.Main!.VertShader,
                            Name = "main"
                        },
                        new PipelineShaderStageCreateInfo
                        {
                            Stage = ShaderStageFlags.Fragment,
                            Module = RenderEngine.ShaderComponent.Main!.FragShader,
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
            RenderPass = RenderEngine.DeviceComponent.Device!.CreateRenderPass(
                new AttachmentDescription[]
                {
                    new()
                    {
                        Format = RenderEngine.SwapChainComponent.SwapChainFormat!.Value,
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
                        Format = RenderEngine.ImageComponent.FindDepthFormat(),
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
            DescriptorSetLayout = RenderEngine.DeviceComponent.Device!.CreateDescriptorSetLayout(
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
                        DescriptorCount = TextureComponent.MAX_TEXTURE_SAMPLERS_IN_SHADER,
                        DescriptorType = DescriptorType.CombinedImageSampler,
                        StageFlags = ShaderStageFlags.Fragment,
                    }
                });
        }

        public void CreateDescriptorPool()
        {
            DescriptorPool = RenderEngine.DeviceComponent.Device!.CreateDescriptorPool(
                10000,
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
                        DescriptorCount = TextureComponent.MAX_TEXTURE_SAMPLERS_IN_SHADER,
                        Type = DescriptorType.CombinedImageSampler
                    }
                });
        }

        public void CreateDescriptorSet()
        {
            DescriptorSet = RenderEngine.DeviceComponent.Device!.AllocateDescriptorSets(DescriptorPool, DescriptorSetLayout).Single();

            RenderEngine.DeviceComponent.Device.UpdateDescriptorSets(
                new WriteDescriptorSet[]
                {
                    new()
                    {
                        BufferInfo = new[]
                        {
                            new DescriptorBufferInfo
                            {
                                Buffer = RenderEngine.ShaderComponent.ViewProj.Uniform.Buffer,
                                Offset = 0,
                                Range = RenderEngine.ShaderComponent.ViewProj.Uniform.SizeOfT
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
                        BufferInfo = new[]
                        {
                            new DescriptorBufferInfo
                            {
                                Buffer = RenderEngine.ShaderComponent.UniformModels.Uniform.Buffer,
                                Offset = 0,
                                Range = RenderEngine.ShaderComponent.UniformModels.Uniform.SizeOfT
                            }
                        },
                        DescriptorCount = 1,
                        DestinationSet = DescriptorSet,
                        DestinationBinding = 1,
                        DestinationArrayElement = 0,
                        DescriptorType = DescriptorType.UniformBufferDynamic
                    },
                    new()
                    {
                        ImageInfo = RenderEngine.TextureComponent.TextureSamplerImageViews,
                        DescriptorCount = TextureComponent.MAX_TEXTURE_SAMPLERS_IN_SHADER,
                        DestinationSet = DescriptorSet,
                        DestinationBinding = 2,
                        DestinationArrayElement = 0,
                        DescriptorType = DescriptorType.CombinedImageSampler,
                    }
                }, null);
        }

        /// <inheritdoc />
        public bool Created { get; private set; }

        public void EnsureExists()
        {
            if (Created) return;

            RenderEngine.DeviceComponent.EnsureDevicesExist();
            RenderEngine.SwapChainComponent.EnsureSwapChainExists();
            RenderEngine.ShaderComponent.EnsureShaderModulesExists();
            RenderEngine.ShaderComponent.UniformModels.EnsureExists();
            RenderEngine.ShaderComponent.ViewProj.EnsureExists();
            RenderEngine.TextureComponent.EnsureDefaultImagesExists();

            CreateRenderPass();
            CreateDescriptorSetLayout();
            CreateGraphicsPipeline();
            CreateDescriptorPool();
            CreateDescriptorSet();

            Created = true;
        }
    }
}
