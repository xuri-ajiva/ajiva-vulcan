using System;
using System.Collections.Generic;
using ajiva.Components;
using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Ecs.Utils;
using ajiva.Models;
using ajiva.Models.Buffer;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;
using SharpVk.Glfw.extras;

namespace ajiva.Systems.VulcanEngine.Layer
{
    public interface IAjivaLayer<T> : IAjivaLayer, IDisposable where T : unmanaged
    {
        new List<IAjivaLayerRenderSystem<T>> LayerRenderComponentSystems { get; }
        public IAChangeAwareBackupBufferOfT<T> LayerUniform { get; }
        public void AddLayer(IAjivaLayerRenderSystem<T> layer);
    }
    public interface IAjivaLayer
    {
        List<IAjivaLayerRenderSystem> LayerRenderComponentSystems { get; }
        AjivaVulkanPipeline PipelineLayer { get; }
        ClearValue[] ClearValues { get;  }

        RenderPassLayer CreateRenderPassLayer(SwapChainLayer swapChainLayer);
    }
}
