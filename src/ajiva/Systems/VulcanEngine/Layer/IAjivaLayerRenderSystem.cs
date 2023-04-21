using Ajiva.Systems.VulcanEngine.Layers;

namespace Ajiva.Systems.VulcanEngine.Layer;

public interface IAjivaLayerRenderSystem
{
    public long DataVersion { get; }
    void DrawComponents(RenderLayerGuard renderGuard, CancellationToken cancellationToken);

    void UpdateGraphicsPipelineLayer();
    
    RenderTarget RenderTarget { get; set; }
}
public interface IAjivaLayerRenderSystem<TParent> : IAjivaLayerRenderSystem, IDisposable where TParent : unmanaged
{
    IAjivaLayer<TParent> AjivaLayer { get; set; }
}
