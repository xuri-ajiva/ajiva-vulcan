using System;
using System.Threading;
using ajiva.Systems.VulcanEngine.Layers;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Utils.Changing;

namespace ajiva.Systems.VulcanEngine.Layer
{
    public interface IAjivaLayerRenderSystem
    {
        public IChangingObserver<IAjivaLayerRenderSystem> GraphicsDataChanged { get; }
        void DrawComponents(RenderLayerGuard renderGuard, CancellationToken cancellationToken);

        GraphicsPipelineLayer CreateGraphicsPipelineLayer(RenderPassLayer renderPassLayer);

        public Reactive<bool> Render { get; }
    }
    public interface IAjivaLayerRenderSystem<TParent> : IAjivaLayerRenderSystem, IDisposable where TParent : unmanaged
    {
        IAjivaLayer<TParent> AjivaLayer { get; set; }
    }
}
