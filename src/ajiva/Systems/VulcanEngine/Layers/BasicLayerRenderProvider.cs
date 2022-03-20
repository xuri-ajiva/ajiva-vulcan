using ajiva.Systems.VulcanEngine.Layer;

namespace ajiva.Systems.VulcanEngine.Layers;

public class BasicLayerRenderProvider : DisposingLogger
{
    private readonly AjivaLayerRenderer ajivaLayerRenderer;
    private readonly IAjivaLayerRenderSystem layer;
    public RenderTarget RenderTarget => layer.RenderTarget;

    public BasicLayerRenderProvider(AjivaLayerRenderer ajivaLayerRenderer, IAjivaLayerRenderSystem layer)
    {
        this.ajivaLayerRenderer = ajivaLayerRenderer;
        this.layer = layer;
    }

    private long _lastVersion = -1;

    public RenderBuffer GetLatestCommandBuffer()
    {
        if (CurrentBuffer is not null && _lastVersion == layer.DataVersion) return CurrentBuffer;
        
        ajivaLayerRenderer.CommandBufferPool.ReturnBuffer(CurrentBuffer);
        CurrentBuffer = ajivaLayerRenderer.CommandBufferPool.GetNewBuffer();
        FillBuffer(CurrentBuffer);

        return CurrentBuffer;
    }

    private RenderBuffer? CurrentBuffer { get; set; }

    private void FillBuffer(RenderBuffer buffer)
    {
        var renderLayerGuard = new RenderLayerGuard(buffer, layer.RenderTarget, ajivaLayerRenderer);
        CommandBufferPool.BeginRecordeRenderBuffer(
            buffer.CommandBuffer,
            layer.RenderTarget.ViewPortInfo,
            renderLayerGuard,
            CancellationToken.None
        );
        layer.DrawComponents(renderLayerGuard, CancellationToken.None);
        CommandBufferPool.EndRecordeRenderBuffer(buffer.CommandBuffer);
    }
}
