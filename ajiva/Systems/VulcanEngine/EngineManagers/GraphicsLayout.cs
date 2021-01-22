using System.Linq;
using System.Runtime.CompilerServices;
using ajiva.Helpers;
using ajiva.Models;
using ajiva.Systems.VulcanEngine.Engine;
using SharpVk;
using SharpVk.Khronos;

namespace ajiva.Systems.VulcanEngine.EngineManagers
{
    public class GraphicsLayout : ThreadSaveCreatable
    {
        private readonly IRenderEngine renderEngine;
        private IRenderEngine RenderEngine => renderEngine;
        private PipelineLayout? PipelineLayout { get; set; }
        private RenderPass? RenderPass { get; set; }
        private Pipeline? Pipeline { get; set; }

        private DescriptorPool? DescriptorPool { get; set; }
        private DescriptorSetLayout? DescriptorSetLayout { get; set; }
        private DescriptorSet? DescriptorSet { get; set; }

        private Framebuffer[]? FrameBuffers { get; set; }
        private CommandBuffer[]? CommandBuffers { get; set; }

        public Swapchain? SwapChain { get; private set; }
        public Format? SwapChainFormat { get; private set; }
        public Extent2D? SwapChainExtent { get; private set; }
        public AImage[]? SwapChainImage { get; private set; }

        public GraphicsLayout(IRenderEngine renderEngine)
        {
            this.renderEngine = renderEngine;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            PipelineLayout?.Dispose();
            RenderPass?.Dispose();
            Pipeline?.Dispose();
            DescriptorPool?.Dispose();
            DescriptorSetLayout?.Dispose();

            renderEngine.DeviceComponent.CommandPool?.FreeCommandBuffers(CommandBuffers);
            CommandBuffers = null;

            if (FrameBuffers != null)
                foreach (var frameBuffer in FrameBuffers)
                    frameBuffer.Dispose();
            FrameBuffers = null;

            if (SwapChainImage != null)
                foreach (var aImage in SwapChainImage)
                    aImage.Dispose();
            SwapChainImage = null;

            SwapChain?.Dispose();
            SwapChain = null;

            SwapChainExtent = null;
            SwapChainFormat = null;
        }

        #region Swapchain

        public void EnsureSwapChainExists()
        {
            renderEngine.DeviceComponent.EnsureDevicesExist();

            var swapChainSupport = RenderEngine.DeviceComponent.PhysicalDevice!.QuerySwapChainSupport(renderEngine.Window.Surface!);
            var extent = swapChainSupport.Capabilities.ChooseSwapExtent(renderEngine.Window.SurfaceExtent);
            var surfaceFormat = swapChainSupport.Formats.ChooseSwapSurfaceFormat();

            if (SwapChain == null) CreateSwapChain(swapChainSupport, surfaceFormat, extent);
            if (SwapChainImage == null) CreateSwapchainImages();

            SwapChainFormat ??= surfaceFormat.Format;
            SwapChainExtent ??= extent;

            //directly create the views sow we dont forget it, and reduce dependency
            if (SwapChainImage!.Any(x => x.View == null)) CreateImageViews();
        }

        private void CreateSwapChain(Extensions.SwapChainSupportDetails swapChainSupport, SurfaceFormat surfaceFormat, Extent2D extent)
        {
            var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
            if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount)
            {
                imageCount = swapChainSupport.Capabilities.MaxImageCount;
            }

            var queueFamilies = RenderEngine.DeviceComponent.PhysicalDevice!.FindQueueFamilies(renderEngine.Window.Surface!);

            var queueFamilyIndices = queueFamilies.Indices.ToArray();

            SwapChain = RenderEngine.DeviceComponent.Device.CreateSwapchain(RenderEngine.Window.Surface,
                imageCount,
                surfaceFormat.Format,
                surfaceFormat.ColorSpace,
                extent,
                1,
                ImageUsageFlags.ColorAttachment,
                queueFamilyIndices.Length == 1
                    ? SharingMode.Exclusive
                    : SharingMode.Concurrent,
                queueFamilyIndices,
                swapChainSupport.Capabilities.CurrentTransform,
                CompositeAlphaFlags.Opaque,
                swapChainSupport.PresentModes.ChooseSwapPresentMode(),
                true,
                SwapChain);
        }

        private void CreateSwapchainImages()
        {
            SwapChainImage = SwapChain!.GetImages().Select(x => new AImage(false)
            {
                Image = x
            }).ToArray();
        }

        private void CreateImageViews()
        {
            foreach (var image in SwapChainImage!)
            {
                image.View ??= RenderEngine.ImageComponent.CreateImageView(image.Image!, SwapChainFormat!.Value, ImageAspectFlags.Color);
            }
        }

#endregion
#region PoplineAndRenderPase

        private void CreateRenderPass()
        {
            RenderPass = renderEngine.DeviceComponent.Device!.CreateRenderPass(
                new AttachmentDescription[]
                {
                    new()
                    {
                        Format = SwapChainFormat!.Value,
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
                        Format = renderEngine.ImageComponent.FindDepthFormat(),
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

        private void CreateDescriptorSetLayout()
        {
            DescriptorSetLayout = renderEngine.DeviceComponent.Device!.CreateDescriptorSetLayout(
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

        private void CreateGraphicsPipeline()
        {
            var bindingDescription = Vertex.GetBindingDescription();
            var attributeDescriptions = Vertex.GetAttributeDescriptions();

            PipelineLayout = renderEngine.DeviceComponent.Device!.CreatePipelineLayout(DescriptorSetLayout, null);

            Pipeline = renderEngine.DeviceComponent.Device.CreateGraphicsPipelines(null, new[]
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
                                Width = SwapChainExtent!.Value.Width,
                                Height = SwapChainExtent!.Value.Height,
                                MaxDepth = 1,
                                MinDepth = 0,
                            }
                        },
                        Scissors = new[]
                        {
                            new Rect2D
                            {
                                Offset = Offset2D.Zero,
                                Extent = SwapChainExtent!.Value
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
                            Module = renderEngine.ShaderComponent.Main!.VertShader,
                            Name = "main"
                        },
                        new PipelineShaderStageCreateInfo
                        {
                            Stage = ShaderStageFlags.Fragment,
                            Module = renderEngine.ShaderComponent.Main!.FragShader,
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

#endregion

#region Descriptor

        private void CreateDescriptorPool()
        {
            DescriptorPool = renderEngine.DeviceComponent.Device!.CreateDescriptorPool(
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

        private void CreateDescriptorSet()
        {
            DescriptorSet = renderEngine.DeviceComponent.Device!.AllocateDescriptorSets(DescriptorPool, DescriptorSetLayout).Single();

            renderEngine.DeviceComponent.Device.UpdateDescriptorSets(
                new WriteDescriptorSet[]
                {
                    new()
                    {
                        BufferInfo = new[]
                        {
                            new DescriptorBufferInfo
                            {
                                Buffer = renderEngine.ShaderComponent.ViewProj.Uniform.Buffer,
                                Offset = 0,
                                Range = renderEngine.ShaderComponent.ViewProj.Uniform.SizeOfT
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
                                Buffer = renderEngine.ShaderComponent.UniformModels.Uniform.Buffer,
                                Offset = 0,
                                Range = renderEngine.ShaderComponent.UniformModels.Uniform.SizeOfT
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
                        ImageInfo = renderEngine.TextureComponent.TextureSamplerImageViews,
                        DescriptorCount = TextureComponent.MAX_TEXTURE_SAMPLERS_IN_SHADER,
                        DestinationSet = DescriptorSet,
                        DestinationBinding = 2,
                        DestinationArrayElement = 0,
                        DescriptorType = DescriptorType.CombinedImageSampler,
                    }
                }, null);
        }

#endregion

#region DrawBuffer

        private void CreateFrameBuffers()
        {
            Framebuffer MakeFrameBuffer(ImageView imageView) => renderEngine.DeviceComponent.Device!.CreateFramebuffer(RenderPass,
                new[]
                {
                    imageView, renderEngine.ImageComponent.DepthImage!.View
                },
                SwapChainExtent!.Value.Width,
                SwapChainExtent!.Value.Height,
                1);

            FrameBuffers ??= SwapChainImage!.Select(x => MakeFrameBuffer(x.View!)).ToArray();
        }

        private void CreateCommandBuffers()
        {
            renderEngine.DeviceComponent.EnsureCommandPoolsExists();

            renderEngine.DeviceComponent.CommandPool!.Reset(CommandPoolResetFlags.ReleaseResources);

            CommandBuffers ??= renderEngine.DeviceComponent.Device!.AllocateCommandBuffers(renderEngine.DeviceComponent.CommandPool, CommandBufferLevel.Primary, (uint)FrameBuffers!.Length);

            for (var index = 0; index < FrameBuffers!.Length; index++)
            {
                var commandBuffer = CommandBuffers[index];

                commandBuffer.Begin(CommandBufferUsageFlags.SimultaneousUse);

                commandBuffer.BeginRenderPass(RenderPass,
                    FrameBuffers[index],
                    new(new(), SwapChainExtent!.Value),
                    new ClearValue[]
                    {
                        new ClearColorValue(.1f, .1f, .1f, 1), new ClearDepthStencilValue(1, 0)
                    },
                    SubpassContents.Inline);

                commandBuffer.BindPipeline(PipelineBindPoint.Graphics, Pipeline);

                foreach (var (renderAble, _) in renderEngine.ComponentEntityMap.Where(x => x.Key.Render))
                {
                    ATrace.Assert(renderAble.Mesh != null, "renderAble.Mesh != null");
                    renderAble.Mesh.Bind(commandBuffer);

                    commandBuffer.BindDescriptorSets(PipelineBindPoint.Graphics, PipelineLayout, 0, DescriptorSet, renderAble.Id * (uint)Unsafe.SizeOf<UniformModel>());

                    renderAble.Mesh.DrawIndexed(commandBuffer);
                }

                commandBuffer.EndRenderPass();

                commandBuffer.End();
            }
        }

#endregion

        /// <inheritdoc />
        protected override void Create()
        {
            renderEngine.DeviceComponent.EnsureDevicesExist();
            EnsureSwapChainExists();
            renderEngine.ShaderComponent.EnsureShaderModulesExists();
            renderEngine.ShaderComponent.UniformModels.EnsureExists();
            renderEngine.ShaderComponent.ViewProj.EnsureExists();
            renderEngine.TextureComponent.EnsureDefaultImagesExists();

            CreateRenderPass();

            CreateDescriptorSetLayout();
            CreateGraphicsPipeline();

            CreateDescriptorPool();
            CreateDescriptorSet();

            CreateFrameBuffers();
            CreateCommandBuffers();
        }
    }
}
