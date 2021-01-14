using System.Linq;
using System.Runtime.CompilerServices;
using ajiva.Engine;
using ajiva.Models;
using SharpVk;

namespace ajiva.EngineManagers
{
    public class GraphicsComponent : RenderEngineComponent
    {
        public PipelineLayout PipelineLayout { get; private set; }
        public RenderPass RenderPass { get; private set; }
        public Pipeline Pipeline { get; private set; }

        public DescriptorPool DescriptorPool { get; private set; }
        public DescriptorSetLayout DescriptorSetLayout { get; private set; }
        public DescriptorSet DescriptorSet { get; private set; }

        public GraphicsComponent(IRenderEngine renderEngine) : base(renderEngine)
        {
            PipelineLayout = null!;
            RenderPass = null!;
            Pipeline = null!;
            DescriptorPool = null!;
            DescriptorSetLayout = null!;
            DescriptorSet = null!;
        }

        public void CreateGraphicsPipeline()
        {
            var bindingDescription = Vertex.GetBindingDescription();
            var attributeDescriptions = Vertex.GetAttributeDescriptions();

            PipelineLayout = RenderEngine.DeviceComponent.Device.CreatePipelineLayout(RenderEngine.GraphicsComponent.DescriptorSetLayout, null);

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
                                Width = RenderEngine.SwapChainComponent.SwapChainExtent.Width,
                                Height = RenderEngine.SwapChainComponent.SwapChainExtent.Height,
                                MaxDepth = 1,
                                MinDepth = 0,
                            }
                        },
                        Scissors = new[]
                        {
                            new Rect2D
                            {
                                Offset = new Offset2D(),
                                Extent = RenderEngine.SwapChainComponent.SwapChainExtent
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
                            Module = RenderEngine.ShaderComponent.Main.VertShader,
                            Name = "main"
                        },
                        new PipelineShaderStageCreateInfo
                        {
                            Stage = ShaderStageFlags.Fragment,
                            Module = RenderEngine.ShaderComponent.Main.FragShader,
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
            RenderPass = RenderEngine.DeviceComponent.Device.CreateRenderPass(
                new AttachmentDescription[]
                {
                    new()
                    {
                        Format = RenderEngine.SwapChainComponent.SwapChainFormat,
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
            DescriptorSetLayout = RenderEngine.DeviceComponent.Device.CreateDescriptorSetLayout(
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
                        DescriptorType = DescriptorType.UniformBuffer,
                        StageFlags = ShaderStageFlags.Vertex,
                        DescriptorCount = 1
                    },
                    new()
                    {
                        Binding = 2,
                        DescriptorCount = 1,
                        DescriptorType = DescriptorType.CombinedImageSampler,
                        StageFlags = ShaderStageFlags.Fragment,
                    }
                });
        }

        public void CreateDescriptorPool()
        {
            DescriptorPool = RenderEngine.DeviceComponent.Device.CreateDescriptorPool(
                1,
                new DescriptorPoolSize[]
                {
                    new()
                    {
                        DescriptorCount = 2,
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
            DescriptorSet = RenderEngine.DeviceComponent.Device.AllocateDescriptorSets(DescriptorPool, DescriptorSetLayout).Single();

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
                                Range = (ulong)Unsafe.SizeOf<UniformViewProj>()
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
                                Range = (ulong)Unsafe.SizeOf<UniformModel>()
                            }
                        },
                        DescriptorCount = 1,
                        DestinationSet = DescriptorSet,
                        DestinationBinding = 1,
                        DestinationArrayElement = 0,
                        DescriptorType = DescriptorType.UniformBuffer
                    },
                    new()
                    {
                        ImageInfo = new[]
                        {
                            new DescriptorImageInfo // todo make modular
                            {
                                Sampler = RenderEngine.TextureComponent.Logo.Sampler,
                                ImageView = RenderEngine.TextureComponent.Logo.Image.View,

                                //Sampler = renderEngine.textureSampler,
                                //ImageView = renderEngine.textureImageView,
                                ImageLayout = ImageLayout.ShaderReadOnlyOptimal
                            }
                        },
                        DescriptorCount = 1,
                        DestinationSet = DescriptorSet,
                        DestinationBinding = 2,
                        DestinationArrayElement = 0,
                        DescriptorType = DescriptorType.CombinedImageSampler,
                    }
                }, null);
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            PipelineLayout.Dispose();
            RenderPass.Dispose();
            Pipeline.Dispose();
            DescriptorPool.Dispose();
            DescriptorSetLayout.Dispose();
        }
    }
}
