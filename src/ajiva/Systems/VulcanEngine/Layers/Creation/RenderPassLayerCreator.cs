namespace ajiva.Systems.VulcanEngine.Layers.Creation;

/*
public static class RenderPassLayerCreator
{
    /*
    public static RenderPassLayer DefaultChecked(SwapChainLayer swapChainLayer, DeviceSystem deviceSystem, AImage depthImage, Format depthFormat, ClearValue[] clearValues)
    {
        if (deviceSystem.Device is null || deviceSystem.PhysicalDevice is null) throw new NotInitializedException(nameof(deviceSystem), deviceSystem);
        return Default(swapChainLayer, deviceSystem, depthImage, depthFormat, clearValues);
    }

    public static RenderPassLayer Default(SwapChainLayer swapChainLayer, DeviceSystem deviceSystem, AImage depthImage, Format depthFormat, ClearValue[] clearValues)
    {
        var renderPass = deviceSystem.Device!.CreateRenderPass(new[]
            {
                new AttachmentDescription(AttachmentDescriptionFlags.None,
                    swapChainLayer.SwapChainFormat,
                    SampleCountFlags.SampleCount1,
                    AttachmentLoadOp.Clear,
                    AttachmentStoreOp.Store,
                    AttachmentLoadOp.DontCare,
                    AttachmentStoreOp.DontCare,
                    ImageLayout.Undefined,
                    ImageLayout.General),
                new AttachmentDescription(AttachmentDescriptionFlags.None,
                    depthFormat,
                    SampleCountFlags.SampleCount1,
                    AttachmentLoadOp.Clear,
                    AttachmentStoreOp.DontCare,
                    AttachmentLoadOp.DontCare,
                    AttachmentStoreOp.DontCare,
                    ImageLayout.Undefined,
                    ImageLayout.DepthStencilAttachmentOptimal)
            },
            new SubpassDescription
            {
                DepthStencilAttachment = new AttachmentReference(1, ImageLayout.DepthStencilAttachmentOptimal),
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
                }
            });

        Framebuffer MakeFrameBuffer(ImageView imageView)
        {
            return deviceSystem.Device.CreateFramebuffer(renderPass,
                new[] { imageView, depthImage.View },
                swapChainLayer.Canvas.Width,
                swapChainLayer.Canvas.Height,
                1);
        }

        var frameBuffers = swapChainLayer.SwapChainImages.Select(x => MakeFrameBuffer(x.View!)).ToArray();

        var renderPassLayer = new RenderPassLayer(swapChainLayer, renderPass, frameBuffers, clearValues);
        swapChainLayer.AddChild(renderPassLayer);
        return renderPassLayer;
    }

    public static RenderPassLayer NoDepth(SwapChainLayer swapChainLayer, DeviceSystem deviceSystem, ClearValue[] clearValues)
    {
        var renderPass = deviceSystem.Device!.CreateRenderPass(new[]
            {
                new AttachmentDescription(AttachmentDescriptionFlags.None,
                    swapChainLayer.SwapChainFormat,
                    SampleCountFlags.SampleCount1,
                    AttachmentLoadOp.Load,
                    AttachmentStoreOp.Store,
                    AttachmentLoadOp.DontCare,
                    AttachmentStoreOp.DontCare,
                    ImageLayout.General,
                    ImageLayout.PresentSource)
            },
            new SubpassDescription
            {
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
                    DestinationAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite
                },
                new SubpassDependency
                {
                    SourceSubpass = 0,
                    DestinationSubpass = Constants.SubpassExternal,
                    SourceStageMask = PipelineStageFlags.ColorAttachmentOutput | PipelineStageFlags.EarlyFragmentTests,
                    SourceAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite,
                    DestinationStageMask = PipelineStageFlags.BottomOfPipe,
                    DestinationAccessMask = AccessFlags.MemoryRead
                }
            });

        Framebuffer MakeFrameBuffer(ImageView imageView)
        {
            return deviceSystem.Device.CreateFramebuffer(renderPass,
                new[] { imageView },
                swapChainLayer.Canvas.Width,
                swapChainLayer.Canvas.Height,
                1);
        }

        var frameBuffers = swapChainLayer.SwapChainImages.Select(x => MakeFrameBuffer(x.View!)).ToArray();

        var renderPassLayer = new RenderPassLayer(swapChainLayer, renderPass, frameBuffers, clearValues);
        swapChainLayer.AddChild(renderPassLayer);
        return renderPassLayer;
    }
}
*/
