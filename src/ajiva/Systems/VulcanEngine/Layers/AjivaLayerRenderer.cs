using System;
using System.Collections.Generic;
using System.Linq;
using ajiva.Models;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers.Creation;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;
using SharpVk;
using SharpVk.Khronos;

namespace ajiva.Systems.VulcanEngine.Layers
{
    public class AjivaLayerRenderer : DisposingLogger
    {
        public SwapChainLayer SwapChainLayer { get; set; }
        public Semaphore ImageAvailable { get; }
        public Semaphore RenderFinished { get; }
        private object bufferLock = new object();

        /// <inheritdoc />
        public AjivaLayerRenderer(Semaphore imageAvailable, Semaphore renderFinished)
        {
            ImageAvailable = imageAvailable;
            RenderFinished = renderFinished;
        }

        public Dictionary<IAjivaLayer, AjivaLayerData> LayerMaps { get; set; } = new();
        public class AjivaLayerData
        {
            public RenderPassLayer RenderPass;

            public AjivaLayerData(RenderPassLayer renderPass)
            {
                RenderPass = renderPass;
            }

            public Dictionary<IAjivaLayerRenderSystem, AjivaLayerRenderSystemData> LayerMaps { get; } = new();
            public class AjivaLayerRenderSystemData
            {
                public GraphicsPipelineLayer GraphicsPipeline;

                public AjivaLayerRenderSystemData(GraphicsPipelineLayer graphicsPipeline)
                {
                    GraphicsPipeline = graphicsPipeline;
                }
            }
        }

        public void PrepareRenderSubmitInfo(DeviceSystem deviceSystem, Canvas canvas, Dictionary<AjivaVulkanPipeline, IAjivaLayer> layers)
        {
            lock (bufferLock)
            {
                PrepareRenderSubmitInfoLocked(deviceSystem, canvas, layers);
            }
        }

        private void PrepareRenderSubmitInfoLocked(DeviceSystem deviceSystem, Canvas canvas, Dictionary<AjivaVulkanPipeline, IAjivaLayer> layers)
        {
            SwapChainLayer?.Dispose();
            LayerMaps.Clear();
            SwapChainLayer = SwapChainLayerCreator.Default(deviceSystem, canvas);

            byte mostPasses = 0;
            foreach (var ajivaLayer in layers.Values)
            {
                var renderPassLayer = ajivaLayer.CreateRenderPassLayer(SwapChainLayer);
                var data = new AjivaLayerData(renderPassLayer);

                foreach (var layer in ajivaLayer.LayerRenderComponentSystems)
                {
                    var graphicsPipelineLayer = layer.CreateGraphicsPipelineLayer(data.RenderPass);
                    data.LayerMaps.Add(layer, new AjivaLayerData.AjivaLayerRenderSystemData(graphicsPipelineLayer));
                }
                mostPasses = Math.Max((byte)data.RenderPass.FrameBuffers.Length, mostPasses);
                LayerMaps.Add(ajivaLayer, data);
            }
            SubmitInfo[] submitInfos = new SubmitInfo[mostPasses];
            for (var i = 0; i < mostPasses; i++)
            {
                submitInfos[i] = new SubmitInfo
                {
                    CommandBuffers = LayerMaps.Values.Select(data => data.RenderPass.RenderBuffers[i]).ToArray(),
                    SignalSemaphores = new[]
                    {
                        RenderFinished
                    },
                    WaitDestinationStageMask = new[]
                    {
                        PipelineStageFlags.ColorAttachmentOutput
                    },
                    WaitSemaphores = new[]
                    {
                        ImageAvailable
                    }
                };
            }

            ResultsCache = new Result[1];

            SubmitInfoCache = submitInfos;
        }

        public void FillBuffers()
        {
            lock (bufferLock)
            {
                RenderLayerGuard guard = new();
                foreach (var (ajivaLayer, data) in LayerMaps)
                {
                    for (var i = 0; i < data.RenderPass.FrameBuffers.Length; i++)
                    {
                        var framebuffer = data.RenderPass.FrameBuffers[i];
                        var commandBuffer = data.RenderPass.RenderBuffers[i];
                        commandBuffer.Reset();

                        commandBuffer.Begin(CommandBufferUsageFlags.SimultaneousUse);

                        commandBuffer.BeginRenderPass(data.RenderPass.RenderPass,
                            framebuffer,
                            SwapChainLayer.Canvas.Rect,
                            ajivaLayer.ClearValues,
                            SubpassContents.Inline);

                        foreach (var (ajivaLayerRenderSystem, ajivaLayerRenderSystemData) in data.LayerMaps)
                        {
                            commandBuffer.BindPipeline(PipelineBindPoint.Graphics, ajivaLayerRenderSystemData.GraphicsPipeline.Pipeline);
                            guard.Pipeline = ajivaLayerRenderSystemData.GraphicsPipeline;
                            guard.Buffer = commandBuffer;
                            ajivaLayerRenderSystem.DrawComponents(guard);
                        }

                        commandBuffer.EndRenderPass();
                        commandBuffer.End();
                    }
                }
            }
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

            var nextImage = SwapChainLayer.SwapChain.AcquireNextImage(uint.MaxValue, ImageAvailable, null);

            graphicsQueue!.Submit(SubmitInfoCache[nextImage], null);

            presentQueue.Present(RenderFinished, SwapChainLayer.SwapChain, nextImage, ResultsCache);
        }

        public Result[] ResultsCache { get; private set; }
        public SubmitInfo[] SubmitInfoCache { get; private set; }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            base.ReleaseUnmanagedResources(disposing);
            SwapChainLayer?.Dispose();
            ImageAvailable.Dispose();
            RenderFinished.Dispose();
            LayerMaps.Clear();
            LayerMaps = null!;
            SubmitInfoCache = null!;
            ResultsCache = null!;
        }
    }
    public class RenderLayerGuard
    {
        public void BindDescriptor(uint dynamicOffset)
        {
            Buffer.BindDescriptorSets(PipelineBindPoint.Graphics, Pipeline.PipelineLayout, 0, Pipeline.DescriptorSet, dynamicOffset);
        }

        public CommandBuffer Buffer { get; set; }
        public GraphicsPipelineLayer Pipeline { get; set; }
    }
}
