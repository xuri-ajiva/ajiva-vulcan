using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Layers
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns>The Old CommandBuffers</returns>
    public delegate void OnBufferReadyDelegate(RenderBuffer renderBuffer, int systemIndex);

    public class RenderBuffer
    {
        public RenderBuffer(CommandBuffer[] commandBuffers, long version)
        {
            this.CommandBuffers = commandBuffers;
            this.Version = version;
        }

        public CommandBuffer[] CommandBuffers { get; set; }
        public long Version { get; set; }
    }

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
}