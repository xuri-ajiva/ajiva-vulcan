using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ajiva.Models;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers.Creation;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;
using SharpVk;
using SharpVk.Khronos;
using Semaphore = SharpVk.Semaphore;

namespace ajiva.Systems.VulcanEngine.Layers
{
    public class AjivaLayerRenderer : DisposingLogger
    {
        internal readonly DeviceSystem DeviceSystem;
        internal readonly Canvas Canvas;
        private SwapChainLayer swapChainLayer;
        private readonly Semaphore imageAvailable;
        private readonly Semaphore renderFinished;
        private readonly object bufferLock = new object();
        private readonly object submitInfoLock = new object();
        private readonly Fence fence;

        public AjivaLayerRenderer(DeviceSystem deviceSystem, Canvas canvas)
        {
            this.DeviceSystem = deviceSystem;
            this.Canvas = canvas;
            imageAvailable = deviceSystem.Device!.CreateSemaphore()!;
            renderFinished = deviceSystem.Device!.CreateSemaphore()!;
            fence = deviceSystem.Device.CreateFence();
        }

        public List<DynamicLayerAjivaLayerRenderSystemData> DynamicLayerSystemData { get; } = new();

        public void Update(IAjivaLayerRenderSystem ajivaLayerRenderSystem)
        {
            foreach (var systemData in DynamicLayerSystemData)
            {
                if (systemData.AjivaLayerRenderSystem == ajivaLayerRenderSystem)
                {
                    systemData.FillNextBufferAsync();
                }
            }
        }

        public void Init(IList<IAjivaLayer> layers)
        {
            ReCreateSwapchainLayer();
            DeleteDynamicLayerData();
            BuildDynamicLayerSystemData(layers);
            CreateSubmitInfo();
            ForceFillBuffers();
        }

        public void ForceFillBuffers()
        {
            foreach (var systemData in DynamicLayerSystemData)
            {
                systemData.FillNextBufferBlocking(CancellationToken.None);
            }
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
                var ajivaLayer = layers[layerIndex];
                for (var layerRenderComponentSystemsIndex = 0; layerRenderComponentSystemsIndex < ajivaLayer.LayerRenderComponentSystems.Count; layerRenderComponentSystemsIndex++)
                {
                    var layer = ajivaLayer.LayerRenderComponentSystems[layerRenderComponentSystemsIndex];
                    var renderPassLayer = ajivaLayer.CreateRenderPassLayer(swapChainLayer,
                        new PositionAndMax(layerIndex, 0, layers.Count - 1),
                        new PositionAndMax(layerRenderComponentSystemsIndex, 0, ajivaLayer.LayerRenderComponentSystems.Count - 1));
                    var graphicsPipelineLayer = layer.CreateGraphicsPipelineLayer(renderPassLayer);
                    DynamicLayerSystemData.Add(new DynamicLayerAjivaLayerRenderSystemData(systemIndex++, this, renderPassLayer, graphicsPipelineLayer, ajivaLayer, layer));
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

        private IEnumerable<CommandBuffer> SwapBuffers(RenderBuffer renderBuffer, int systemIndex)
        {
            lock (submitInfoLock)
            {
                //todo multiple buffers per layer to add stuff easy
                for (var i = 0; i < renderBuffer.CommandBuffers.Length; i++)
                {
                    yield return SubmitInfoCache[i].CommandBuffers[systemIndex];
                    SubmitInfoCache[i].CommandBuffers[systemIndex] = renderBuffer.CommandBuffers[i];
                }
            }
        }

        public void CheckBuffersUpToDate()
        {
            foreach (var systemData in DynamicLayerSystemData.Where(x => !x.IsVersionUpToDate && !x.IsBackgroundTaskRunning))
            {
                systemData.FillNextBufferAsync();
            }
        }

        public void UpdateSubmitInfoChecked()
        {
            for (var systemIndex = 0; systemIndex < DynamicLayerSystemData.Count; systemIndex++)
            {
                if (!DynamicLayerSystemData[systemIndex].TryGetUpdatedBuffers(out var update)) continue;

                var oldBuffers = PerformUpdate(update, systemIndex);
                DynamicLayerSystemData[systemIndex].ReturnBuffer(oldBuffers);
            }
        }

        private CommandBuffer[] PerformUpdate(RenderBuffer renderBuffer, int systemIndex)
        {
            return SwapBuffers(renderBuffer, systemIndex).ToArray();
        }

        public void CreateSubmitInfo()
        {
            SubmitInfo[] submitInfos = new SubmitInfo[swapChainLayer.SwapChainImages.Length];
            for (var nextImage = 0; nextImage < submitInfos.Length; nextImage++)
            {
                submitInfos[nextImage] = new SubmitInfo
                {
                    CommandBuffers = new CommandBuffer[DynamicLayerSystemData.Count],
                    SignalSemaphores = new[]
                    {
                        renderFinished
                    },
                    WaitDestinationStageMask = new[]
                    {
                        PipelineStageFlags.ColorAttachmentOutput
                    },
                    WaitSemaphores = new[]
                    {
                        imageAvailable
                    }
                };
            }
            lock (submitInfoLock)
                SubmitInfoCache = submitInfos;

            ResultsCache = new Result[1];
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

                graphicsQueue.Submit(SubmitInfoCache[nextImage], fence);

                presentQueue.Present(renderFinished, swapChainLayer.SwapChain, nextImage, ResultsCache);

                //graphicsQueue.WaitIdle();
                fence.Wait(20_000_000UL); // 20 ms in ns
                fence.Reset();
            }
        }

        public Result[] ResultsCache { get; private set; }
        public SubmitInfo[] SubmitInfoCache { get; private set; }

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
                SubmitInfoCache = null!;
                ResultsCache = null!;
            }
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
