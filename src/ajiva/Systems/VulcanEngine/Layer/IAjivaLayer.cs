﻿using System;
using System.Collections.Generic;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Utils.Changing;

namespace ajiva.Systems.VulcanEngine.Layer
{
    public interface IAjivaLayer<T> : IAjivaLayer, IDisposable where T : unmanaged
    {
        new List<IAjivaLayerRenderSystem<T>> LayerRenderComponentSystems { get; }
        public IAChangeAwareBackupBufferOfT<T> LayerUniform { get; }
    }

    public static class AjivaLayerExtensions
    {
        public static void AddLayer<T>(this IAjivaLayer<T> ajivaLayer, IAjivaLayerRenderSystem<T> ajivaLayerRenderSystem) where T : unmanaged
        {
            ajivaLayerRenderSystem.AjivaLayer = ajivaLayer;
            ajivaLayer.LayerRenderComponentSystems.Add(ajivaLayerRenderSystem);
            ajivaLayer.LayerChanged.Changed();
        }
    }
    public interface IAjivaLayer
    {
        public IChangingObserver<IAjivaLayer> LayerChanged { get; }
        List<IAjivaLayerRenderSystem> LayerRenderComponentSystems { get; }
        AjivaVulkanPipeline PipelineLayer { get; }
        RenderPassLayer CreateRenderPassLayer(SwapChainLayer swapChainLayer, PositionAndMax layerIndex, PositionAndMax layerRenderComponentSystemsIndex);
    }

    public struct PositionAndMax
    {
        public PositionAndMax(int index, int start, int end)
        {
            this.Index = index;
            this.Start = start;
            this.End = end;
        }

        public bool First => Index == Start;
        public bool Last => Index == End;
        public int Index { get; init; }
        public int Start { get; init; }
        public int End { get; init; }
        
    }
}
