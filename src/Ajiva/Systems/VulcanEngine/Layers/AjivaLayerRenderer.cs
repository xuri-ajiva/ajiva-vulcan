using Ajiva.Systems.Assets;
using Ajiva.Systems.VulcanEngine.Interfaces;
using Ajiva.Systems.VulcanEngine.Layer;
using Ajiva.Systems.VulcanEngine.Layers.Creation;
using Ajiva.Systems.VulcanEngine.Layers.Models;
using Ajiva.Systems.VulcanEngine.Systems;
using SharpVk;
using SharpVk.Khronos;
using Semaphore = SharpVk.Semaphore;

namespace Ajiva.Systems.VulcanEngine.Layers;

public class AjivaLayerRenderer : DisposingLogger
{
    private readonly object bufferLock = new object();
    internal readonly Canvas Canvas;
    internal readonly DeviceSystem DeviceSystem;
    public readonly Fence fence;
    public readonly Semaphore imageAvailable;
    public readonly Semaphore renderFinished;
    public readonly object submitInfoLock = new object();
    public SwapChainLayer swapChainLayer;
    public CombinePipeline CombinePipeline;
    public CommandBufferPool CommandBufferPool;
    private readonly ITextureSystem _textureSystem;

    public AjivaLayerRenderer(DeviceSystem deviceSystem, Canvas canvas, CommandBufferPool commandBufferPool, ITextureSystem textureSystem, IAssetManager assetManager)
    {
        DeviceSystem = deviceSystem;
        Canvas = canvas;
        CommandBufferPool = commandBufferPool;
        _textureSystem = textureSystem;
        imageAvailable = deviceSystem.Device!.CreateSemaphore()!;
        renderFinished = deviceSystem.Device!.CreateSemaphore()!;
        fence = deviceSystem.Device.CreateFence();
        CombinePipeline = new CombinePipeline(this, textureSystem, assetManager);
    }

    public List<BasicLayerRenderProvider> DynamicLayerSystemData { get; } = new List<BasicLayerRenderProvider>();

    public Result[] ResultsCache { get; private set; }

    public void Init(IList<IAjivaLayer> layers)
    {
        ReCreateSwapchainLayer();
        DeleteDynamicLayerData();
        BuildDynamicLayerSystemData(layers);
    }

    public void ReCreateSwapchainLayer()
    {
        swapChainLayer?.Dispose();
        swapChainLayer = SwapChainLayerCreator.Default(DeviceSystem, Canvas);
    }

    public void BuildDynamicLayerSystemData(IList<IAjivaLayer> layers)
    {
        var systemIndex = 0;
        for (var layerIndex = 0; layerIndex < layers.Count; layerIndex++)
        {
            var AjivaLayer = layers[layerIndex];
            for (var layerRenderComponentSystemsIndex = 0; layerRenderComponentSystemsIndex < AjivaLayer.LayerRenderComponentSystems.Count; layerRenderComponentSystemsIndex++)
            {
                var layer = AjivaLayer.LayerRenderComponentSystems[layerRenderComponentSystemsIndex];
                layer.RenderTarget = AjivaLayer.CreateRenderPassLayer(swapChainLayer,
                    new PositionAndMax(layerIndex, 0, layers.Count - 1),
                    new PositionAndMax(layerRenderComponentSystemsIndex, 0, AjivaLayer.LayerRenderComponentSystems.Count - 1));
                layer.UpdateGraphicsPipelineLayer();
                DynamicLayerSystemData.Add(new BasicLayerRenderProvider(this, layer));
                systemIndex++;
            }
        }
    }

    private void DeleteDynamicLayerData()
    {
        foreach (var data in DynamicLayerSystemData)
        {
            data.Dispose();
        }
        DynamicLayerSystemData.Clear();
    }

    public void DrawFrame(Queue graphicsQueue, Queue presentQueue)
    {
        lock (bufferLock)
        {
            DrawFrameNoLock(graphicsQueue, presentQueue);
        }
    }

    private void DrawFrameNoLock(Queue graphicsQueue, Queue presentQueue)
    {
        if (Disposed) return;
        lock (submitInfoLock)
        {
            var nextImage = swapChainLayer.SwapChain.AcquireNextImage(uint.MaxValue, imageAvailable, null);

            var submitInfo = GetSubmitInfo(nextImage);

            graphicsQueue.Submit(submitInfo, fence);

            presentQueue.Present(renderFinished, swapChainLayer.SwapChain, nextImage, ResultsCache);

            //graphicsQueue.WaitIdle();
            fence.Wait(20_000_000UL); // 20 ms in ns
            fence.Reset();
        }
    }

    private ArrayProxy<SubmitInfo>? GetSubmitInfo(uint nextImage)
    {
        var commandBuffers = new CommandBuffer[DynamicLayerSystemData.Count + 1];
        for (var i = 0; i < DynamicLayerSystemData.Count; i++)
        {
            commandBuffers[i] = DynamicLayerSystemData[i].GetLatestCommandBuffer().CommandBuffer;
        }

        commandBuffers[^1] = CombinePipeline.Combine(DynamicLayerSystemData, nextImage);

        return new SubmitInfo {
            CommandBuffers = commandBuffers,
            SignalSemaphores = new[] {
                renderFinished
            },
            WaitDestinationStageMask = new[] {
                PipelineStageFlags.ColorAttachmentOutput
            },
            WaitSemaphores = new[] {
                imageAvailable
            }
        };
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        base.ReleaseUnmanagedResources(disposing);

        lock (bufferLock)
        lock (submitInfoLock)
        {
            DeleteDynamicLayerData();

            fence?.Dispose();
            swapChainLayer?.Dispose();
            imageAvailable.Dispose();
            renderFinished.Dispose();
            ResultsCache = null!;
        }
    }
}
