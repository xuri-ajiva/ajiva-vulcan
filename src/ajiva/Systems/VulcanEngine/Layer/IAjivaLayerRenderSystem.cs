using System;
using ajiva.Systems.VulcanEngine.Layers;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Utils.Changing;

namespace ajiva.Systems.VulcanEngine.Layer
{
    public interface IAjivaLayerRenderSystem
    {
        public IChangingObserver<IAjivaLayerRenderSystem> GraphicsDataChanged { get; }
        void DrawComponents(RenderLayerGuard renderGuard);

        GraphicsPipelineLayer CreateGraphicsPipelineLayer(RenderPassLayer renderPassLayer);

        public Reactive<bool> Render { get; }
    }
    public interface IAjivaLayerRenderSystem<TParent> : IAjivaLayerRenderSystem, IDisposable where TParent : unmanaged
    {
        IAjivaLayer<TParent> AjivaLayer { get; set; }
    }
}
