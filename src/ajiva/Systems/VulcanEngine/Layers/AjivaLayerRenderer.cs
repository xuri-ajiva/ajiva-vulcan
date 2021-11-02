using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class DynamicLayerAjivaLayerRenderSystemData
    {
        public DynamicLayerAjivaLayerRenderSystemData(RenderPassLayer renderPass, GraphicsPipelineLayer graphicsPipeline, IAjivaLayer ajivaLayer, IAjivaLayerRenderSystem ajivaLayerRenderSystem)
        {
            RenderPass = renderPass;
            GraphicsPipeline = graphicsPipeline;
            AjivaLayer = ajivaLayer;
            AjivaLayerRenderSystem = ajivaLayerRenderSystem;
            RenderBuffers = new CommandBuffer[]?[Const.Default.BackupBuffers];
        }

        public CommandBuffer[]?[] RenderBuffers { get; }

        public RenderPassLayer RenderPass { get; init; }
        public GraphicsPipelineLayer GraphicsPipeline { get; init; }
        public IAjivaLayerRenderSystem AjivaLayerRenderSystem { get; init; }
        public IAjivaLayer AjivaLayer { get; init; }

        public int CurrentBufferIndex { get; private set; }

        private int AcquireNextBufferIndex()
        {
            CurrentBufferIndex++;
            if (CurrentBufferIndex >= RenderBuffers.Length)
                CurrentBufferIndex = 0;
            return CurrentBufferIndex;
        }

        public void FillNextBuffer(DeviceSystem deviceSystem, Canvas canvas)
        {
            /*
             * backgroundBuffer[j]          backup.count                        // 2 or more
             * swapBuffer[k]     RenderPass.FrameBuffers.Length                 // should be 2
             * layerBuffer[i]    AjivaLayer.LayerRenderComponentSystems.Count   // any length > 1
             */

            var nextBufferIndex = AcquireNextBufferIndex();
            /*foreach (var backgroundBuffer in RenderBuffers)
            {
                foreach (var swapBuffer in backgroundBuffer)
                {
                    foreach (var layerBuffer in swapBuffer)
                    {
                    }
                }
            }*/
            FillBuffer(deviceSystem,
                RenderBuffers[nextBufferIndex] ??= new CommandBuffer[RenderPass.FrameBuffers.Length],
                canvas, new RenderLayerGuard());
        }

        private void FillBuffer(DeviceSystem deviceSystem, IList<CommandBuffer?> swapBuffer, Canvas canvas, RenderLayerGuard guard)
        {
            System.Diagnostics.Debug.Assert(swapBuffer.Count == RenderPass.FrameBuffers.Length, "swapBuffer.Length == RenderPass.FrameBuffers.Length");
            for (var i = 0; i < RenderPass.FrameBuffers.Length; i++)
            {
                var framebuffer = RenderPass.FrameBuffers[i];

                FillBuffer(swapBuffer[i] ??= deviceSystem.AllocateCommandBuffer(CommandBufferLevel.Primary),
                    framebuffer, canvas, guard);
            }
        }

        private void FillBuffer(CommandBuffer commandBuffer, Framebuffer framebuffer, Canvas canvas, RenderLayerGuard guard)
        {
            commandBuffer.Reset();
            commandBuffer.Begin(CommandBufferUsageFlags.SimultaneousUse);
            commandBuffer.BeginRenderPass(RenderPass.RenderPass,
                framebuffer,
                canvas.Rect,
                RenderPass.ClearValues,
                SubpassContents.Inline);
            commandBuffer.BindPipeline(PipelineBindPoint.Graphics, GraphicsPipeline.Pipeline);
            guard.Pipeline = GraphicsPipeline;
            guard.Buffer = commandBuffer;
            AjivaLayerRenderSystem.DrawComponents(guard);
            commandBuffer.EndRenderPass();
            commandBuffer.End();
        }
    }

    public class AjivaLayerRenderer : DisposingLogger
    {
        private readonly DeviceSystem deviceSystem;
        private readonly Canvas canvas;
        public SwapChainLayer SwapChainLayer { get; set; }
        public Semaphore ImageAvailable { get; }
        public Semaphore RenderFinished { get; }
        private object bufferLock = new object();

        public AjivaLayerRenderer(Semaphore imageAvailable, Semaphore renderFinished, DeviceSystem deviceSystem, Canvas canvas)
        {
            this.deviceSystem = deviceSystem;
            this.canvas = canvas;
            ImageAvailable = imageAvailable;
            RenderFinished = renderFinished;
        }

        public List<DynamicLayerAjivaLayerRenderSystemData> DynamicLayerSystemData { get; } = new();

        public void Update(IAjivaLayerRenderSystem ajivaLayerRenderSystem)
        {
            for (var systemIndex = 0; systemIndex < DynamicLayerSystemData.Count; systemIndex++)
            {
                if (DynamicLayerSystemData[systemIndex].AjivaLayerRenderSystem == ajivaLayerRenderSystem)
                {
                    DynamicLayerSystemData[systemIndex].FillNextBuffer(deviceSystem, canvas);
                }
            }
            UpdateSubmitInfo();
        }

        public void Init(IList<IAjivaLayer> layers)
        {
            ReCreateSwapchainLayer();
            BuildDynamicLayerSystemData(layers);
            foreach (var dynamicLayerAjivaLayerRenderSystemData in DynamicLayerSystemData)
            {
                dynamicLayerAjivaLayerRenderSystemData.FillNextBuffer(deviceSystem, canvas);
            }
            CreateSubmitInfo();
            FillBuffers();
        }

        public void FillBuffers()
        {
            for (var systemIndex = 0; systemIndex < DynamicLayerSystemData.Count; systemIndex++)
            {
                DynamicLayerSystemData[systemIndex].FillNextBuffer(deviceSystem, canvas);
            }
            UpdateSubmitInfo();
        }

        public void ReCreateSwapchainLayer()
        {
            SwapChainLayer?.Dispose();
            SwapChainLayer = SwapChainLayerCreator.Default(deviceSystem, canvas);
        }

        public void BuildDynamicLayerSystemData(IList<IAjivaLayer> layers)
        {
            DynamicLayerSystemData.Clear();
            for (var layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                var ajivaLayer = layers[layerIndex];
                for (var layerRenderComponentSystemsIndex = 0; layerRenderComponentSystemsIndex < ajivaLayer.LayerRenderComponentSystems.Count; layerRenderComponentSystemsIndex++)
                {
                    var layer = ajivaLayer.LayerRenderComponentSystems[layerRenderComponentSystemsIndex];
                    var renderPassLayer = ajivaLayer.CreateRenderPassLayer(SwapChainLayer,
                        new PositionAndMax(layerIndex, 0, layers.Count - 1),
                        new PositionAndMax(layerRenderComponentSystemsIndex, 0, ajivaLayer.LayerRenderComponentSystems.Count - 1));
                    var graphicsPipelineLayer = layer.CreateGraphicsPipelineLayer(renderPassLayer);
                    DynamicLayerSystemData.Add(new DynamicLayerAjivaLayerRenderSystemData(renderPassLayer, graphicsPipelineLayer, ajivaLayer, layer));
                }
            }
        }

        public void UpdateSubmitInfo()
        {
            //SubmitInfoCache.Length should be 2
            for (var nextImage = 0; nextImage < SubmitInfoCache.Length; nextImage++)
            {
                SubmitInfoCache[nextImage].CommandBuffers = DynamicLayerSystemData.Select(x => x.RenderBuffers[x.CurrentBufferIndex][nextImage]).ToArray();
            }
        }

        public void CreateSubmitInfo()
        {
            SubmitInfo[] submitInfos = new SubmitInfo[SwapChainLayer.SwapChainImages.Length];
            for (var nextImage = 0; nextImage < submitInfos.Length; nextImage++)
            {
                submitInfos[nextImage] = new SubmitInfo
                {
                    CommandBuffers = DynamicLayerSystemData.Select(x => x.RenderBuffers[x.CurrentBufferIndex][nextImage]).ToArray(), //todo multiple buffers per layer to add stuff easy
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

            var nextImage = SwapChainLayer.SwapChain.AcquireNextImage(uint.MaxValue, ImageAvailable, null);

            graphicsQueue.Submit(SubmitInfoCache[nextImage], null);

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
