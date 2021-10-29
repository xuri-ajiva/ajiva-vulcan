using System;
using ajiva.Systems.VulcanEngine.Layers;
using ajiva.Systems.VulcanEngine.Layers.Models;

namespace ajiva.Systems.VulcanEngine.Layer
{
    public interface IAjivaLayerRenderSystem
    {
        void DrawComponents(RenderLayerGuard renderGuard);

        GraphicsPipelineLayer CreateGraphicsPipelineLayer(RenderPassLayer renderPassLayer);

        public bool Render { get; }
    }
    public interface IAjivaLayerRenderSystem<TParent> : IAjivaLayerRenderSystem, IDisposable where TParent : unmanaged
    {
        IAjivaLayer<TParent> AjivaLayer { get; set; }
    }
}
