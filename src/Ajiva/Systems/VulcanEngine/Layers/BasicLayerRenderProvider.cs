using Ajiva.Systems.VulcanEngine.Layer;

namespace Ajiva.Systems.VulcanEngine.Layers;

public class BasicLayerRenderProvider : DisposingLogger
{
    private readonly long _lastVersion = -1;
    private readonly AjivaLayerRenderer AjivaLayerRenderer;
    private readonly IAjivaLayerRenderSystem layer;

    public BasicLayerRenderProvider(AjivaLayerRenderer AjivaLayerRenderer, IAjivaLayerRenderSystem layer)
    {
        this.AjivaLayerRenderer = AjivaLayerRenderer;
        this.layer = layer;
    }

    public RenderTarget RenderTarget => layer.RenderTarget;

    private RenderBuffer? CurrentBuffer { get; set; }

    public RenderBuffer GetLatestCommandBuffer()
    {
        if (CurrentBuffer is not null && _lastVersion == layer.DataVersion) return CurrentBuffer;

        AjivaLayerRenderer.CommandBufferPool.ReturnBuffer(CurrentBuffer);
        CurrentBuffer = AjivaLayerRenderer.CommandBufferPool.GetNewBuffer();
        FillBuffer(CurrentBuffer);

        return CurrentBuffer;
    }

    private void FillBuffer(RenderBuffer buffer)
    {
        var renderLayerGuard = new RenderLayerGuard(buffer, layer.RenderTarget, AjivaLayerRenderer);
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