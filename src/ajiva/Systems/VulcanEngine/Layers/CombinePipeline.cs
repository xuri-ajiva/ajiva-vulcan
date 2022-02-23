using ajiva.Components;
using ajiva.Components.Media;
using ajiva.Ecs;
using ajiva.Systems.Assets;
using ajiva.Systems.VulcanEngine.Layers.Creation;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Layers;

public class CombinePipeline : DisposingLogger
{
    private readonly AjivaLayerRenderer layerRenderer;
    private readonly IAjivaEcs ecs;

    private FullRenderTarget? fullRenderTarget;

    public CombinePipeline(AjivaLayerRenderer layerRenderer, IAjivaEcs ecs)
    {
        this.layerRenderer = layerRenderer;
        this.ecs = ecs;
    }

    private static DescriptorImageInfo[] CreateDescriptorImageInfo(IReadOnlyList<BasicLayerRenderProvider> dynamicLayerSystemData, DeviceSystem deviceSystem)
    {
        var res = new DescriptorImageInfo[dynamicLayerSystemData.Count];
        for (int i = res.Length - 1; i >= 0; i--)
        {
            res[i] = new DescriptorImageInfo(
                ATexture.CreateTextureSampler(deviceSystem),
                dynamicLayerSystemData[res.Length - 1 - i].RenderTarget.ViewPortInfo.FrameBufferImage.View,
                ImageLayout.General
            );
        }
        return res;
    }

    public CommandBuffer Combine(IReadOnlyList<BasicLayerRenderProvider> dynamicLayerSystemData, uint nextImage)
    {
        if (fullRenderTarget is null)
        {
            CreateFullRenderTarget(dynamicLayerSystemData);
        }

        return FillBuffer(nextImage, dynamicLayerSystemData);
    }

    private void CreateFullRenderTarget(IReadOnlyList<BasicLayerRenderProvider> dynamicLayerSystemData)
    {
        if (fullRenderTarget is not null) return;

        var deviceSystem = layerRenderer.DeviceSystem;
        var swapChainLayer = layerRenderer.swapChainLayer;
        var renderPass = deviceSystem.Device!.CreateRenderPass(
            new[]
            {
                new AttachmentDescription(AttachmentDescriptionFlags.None,
                    swapChainLayer.SwapChainFormat,
                    SampleCountFlags.SampleCount1,
                    AttachmentLoadOp.Clear,
                    AttachmentStoreOp.Store,
                    AttachmentLoadOp.DontCare,
                    AttachmentStoreOp.DontCare,
                    ImageLayout.Undefined,
                    ImageLayout.PresentSource),
            },
            new SubpassDescription
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachments = new[] { new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal) },
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
                    DestinationAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite | AccessFlags.DepthStencilAttachmentRead,
                    DependencyFlags = DependencyFlags.None,
                },
                new SubpassDependency
                {
                    SourceSubpass = 0,
                    DestinationSubpass = Constants.SubpassExternal,
                    SourceStageMask = PipelineStageFlags.ColorAttachmentOutput | PipelineStageFlags.EarlyFragmentTests,
                    SourceAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite | AccessFlags.DepthStencilAttachmentRead,
                    DestinationStageMask = PipelineStageFlags.BottomOfPipe,
                    DestinationAccessMask = AccessFlags.MemoryRead,
                    DependencyFlags = DependencyFlags.None
                }
            });

        var frameBuffers = swapChainLayer.SwapChainImages.Select(x => deviceSystem.Device.CreateFramebuffer(renderPass,
            new[]
            {
                x.View
            },
            swapChainLayer.Canvas.Width,
            swapChainLayer.Canvas.Height,
            1)).ToArray();
        var renderPassLayer = new RenderPassLayer(swapChainLayer, renderPass);

        var descriptorImageInfos = CreateDescriptorImageInfo(dynamicLayerSystemData, deviceSystem);

        var mainShader = Shader.CreateShaderFrom(ecs.Get<AssetManager>(), "combine", layerRenderer.DeviceSystem, "main");
        var graphicsPipelineLayer = GraphicsPipelineLayerCreator.Default(
            swapChainLayer,
            renderPassLayer,
            layerRenderer.DeviceSystem,
            false,
            Array.Empty<VertexInputBindingDescription>(),
            Array.Empty<VertexInputAttributeDescription>(),
            mainShader,
            new[]
            {
                new PipelineDescriptorInfos(DescriptorType.CombinedImageSampler, ShaderStageFlags.Fragment, 1, (uint)descriptorImageInfos.Length, ImageInfo: descriptorImageInfos)
            }
        );

        fullRenderTarget = new FullRenderTarget(swapChainLayer, frameBuffers, renderPassLayer, graphicsPipelineLayer, mainShader, MakeVersion(dynamicLayerSystemData));
    }

    private static uint MakeVersion(IEnumerable<BasicLayerRenderProvider> dynamicLayerSystemData)
    {
        var init = dynamicLayerSystemData.Aggregate(0xF3E397F, (current, provider) => HashCode.Combine(current, provider.GetHashCode()));

        return unchecked((uint)init);
    }

    private RenderBuffer[] cache = Array.Empty<RenderBuffer>();

    private CommandBuffer FillBuffer(uint nextImage, IReadOnlyCollection<BasicLayerRenderProvider> basicLayerRenderProviders)
    {
        var version = MakeVersion(basicLayerRenderProviders);
        if (cache.Length <= nextImage || cache[nextImage].Version != version)
        {
            Array.Resize(ref cache, (int)nextImage + 1);
            layerRenderer.CommandBufferPool.ReturnBuffer(cache[nextImage]);
            cache[nextImage] = CreateBuffer(nextImage, basicLayerRenderProviders.Count);
            cache[nextImage].Version = version;
        }

        return cache[nextImage].CommandBuffer;
    }

    private RenderBuffer CreateBuffer(uint nextImage, int count)
    {
        System.Diagnostics.Debug.Assert(fullRenderTarget != null, nameof(fullRenderTarget) + " != null");

        var renderBuffer = layerRenderer.CommandBufferPool.GetNewBuffer();

        var commandBuffer = renderBuffer.CommandBuffer;
        commandBuffer.Reset();
        commandBuffer.Begin(CommandBufferUsageFlags.SimultaneousUse);
        commandBuffer.BeginRenderPass(fullRenderTarget.RenderPassLayer.RenderPass, fullRenderTarget.FrameBuffers[nextImage], fullRenderTarget.SwapChainLayer.Canvas.Rect, new ClearValue[] { new ClearColorValue(.1f, .1f, .1f, .1f) }, SubpassContents.Inline);
        commandBuffer.SetViewport(0, new Viewport(0, 0, fullRenderTarget.SwapChainLayer.Canvas.Width, fullRenderTarget.SwapChainLayer.Canvas.Height, 0, 1));
        commandBuffer.SetScissor(0, fullRenderTarget.SwapChainLayer.Canvas.Rect);
        commandBuffer.BindPipeline(PipelineBindPoint.Graphics, fullRenderTarget.GraphicsPipelineLayer.Pipeline);
        commandBuffer.BindDescriptorSets(PipelineBindPoint.Graphics, fullRenderTarget.GraphicsPipelineLayer.PipelineLayout, 0, fullRenderTarget.GraphicsPipelineLayer.DescriptorSet, ArrayProxy<uint>.Null);
        commandBuffer.Draw(6, (uint)count, 0, 0);
        commandBuffer.EndRenderPass();
        commandBuffer.End();

        return renderBuffer;
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        base.ReleaseUnmanagedResources(disposing);
        fullRenderTarget?.Dispose();
        foreach (var renderBuffer in cache) 
            layerRenderer.CommandBufferPool.ReturnBuffer(renderBuffer);
    }
}
