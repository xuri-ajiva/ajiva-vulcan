﻿using System.Collections.Generic;
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
        private readonly int id;
        private readonly AjivaLayerRenderer renderer;
        private readonly OnBufferReadyDelegate onBufferReady;

        public DynamicLayerAjivaLayerRenderSystemData(
            int id,
            AjivaLayerRenderer renderer,
            RenderPassLayer renderPass,
            GraphicsPipelineLayer graphicsPipeline,
            IAjivaLayer ajivaLayer,
            IAjivaLayerRenderSystem ajivaLayerRenderSystem,
            OnBufferReadyDelegate onBufferReady
        )
        {
            this.id = id;
            this.renderer = renderer;
            this.onBufferReady = onBufferReady;
            RenderPass = renderPass;
            GraphicsPipeline = graphicsPipeline;
            AjivaLayer = ajivaLayer;
            AjivaLayerRenderSystem = ajivaLayerRenderSystem;
            for (var i = 0; i < Const.Default.BackupBuffers; i++)
            {
                AllocateNewBuffers();
            }
        }
        private List<RenderBuffer> AllocatedBuffers { get; } = new();
        public Queue<RenderBuffer> RenderBuffers { get; } = new();

        public RenderPassLayer RenderPass { get; init; }
        public GraphicsPipelineLayer GraphicsPipeline { get; init; }
        public IAjivaLayerRenderSystem AjivaLayerRenderSystem { get; init; }
        public IAjivaLayer AjivaLayer { get; init; }

        public Task? UpdateTask { get; private set; }
        public CancellationTokenSource TokenSource { get; set; } = new();

        public bool IsBackgroundTaskRunning => UpdateTask is not null;
        public bool IsVersionUpToDate => AjivaLayerRenderSystem.GraphicsDataChanged.Version == CurrentActiveVersion;

        public void FillNextBufferBlocking(CancellationToken cancellationToken)
        {
            FillBuffer(GetNextBuffer(), new RenderLayerGuard(), cancellationToken);
        }
        private void AllocateNewBuffers()
        {
            var buffers = new RenderBuffer(renderer.deviceSystem.AllocateCommandBuffers(CommandBufferLevel.Primary, RenderPass.FrameBuffers.Length, CommandPoolSelector.Background), 0);
            AllocatedBuffers.Add(buffers);
            RenderBuffers.Enqueue(buffers);
        }
        
        private RenderBuffer GetNextBuffer()
        {
            lock (Lock)
            {
                if (!RenderBuffers.Any())
                {
                    AllocateNewBuffers();
                }
                RenderBuffers.TryDequeue(out var result);
                System.Diagnostics.Debug.Assert(result is not null, nameof(result) + " != null");
                return result;
            }
        }

        public readonly object Lock = new();
        public void FillNextBufferAsync()
        {
            lock (this)
            {
                if (UpdateTask is not null)
                {
                    TokenSource.Cancel(true);
                    TokenSource = new CancellationTokenSource();
                }

                UpdateTask = Task.Run(() =>
                {
                    FillNextBufferBlocking(TokenSource.Token);
                    UpdateTask = null;
                }, TokenSource.Token);
            }
        }

        private void FillBuffer(RenderBuffer renderBuffer, RenderLayerGuard guard, CancellationToken cancellationToken)
        {
            lock (renderBuffer)
            {
                var vTmp = AjivaLayerRenderSystem.GraphicsDataChanged.Version;
                if (renderBuffer.Version == vTmp)
                    return;
                System.Diagnostics.Debug.Assert(renderBuffer.CommandBuffers.Length == RenderPass.FrameBuffers.Length, "swapBuffer.Length == RenderPass.FrameBuffers.Length");
                for (var i = 0; i < RenderPass.FrameBuffers.Length; i++)
                {
                    var framebuffer = RenderPass.FrameBuffers[i];

                    FillBuffer(renderBuffer.CommandBuffers[i], framebuffer, guard, cancellationToken);
                    if (cancellationToken.IsCancellationRequested) return;
                }
                PushRenderBuffer(vTmp, renderBuffer);
            }
        }

        public long CurrentActiveVersion { get; set; }

        private void PushRenderBuffer(long version, RenderBuffer renderBuffer)
        {
            lock (Lock)
            {
                renderBuffer.Version = version;
                onBufferReady.Invoke(renderBuffer, id);
                CurrentActiveVersion = version;
            }
        }

        public void ReturnBuffer(CommandBuffer?[] commandBuffers)
        {
            if (commandBuffers.Any(x => x is null))
            {
                FreeBuffers(commandBuffers);
            }
            else
            {
                RenderBuffers.Enqueue(new RenderBuffer(commandBuffers, CurrentActiveVersion));
            }
        }

        private void FreeBuffers(CommandBuffer?[] commandBuffers)
        {
            renderer.deviceSystem.UseCommandPool(x =>
            {
                x.FreeCommandBuffers(commandBuffers.Where(y => y is not null).ToArray());
            }, CommandPoolSelector.Background);
        }

        private void FillBuffer(CommandBuffer commandBuffer, Framebuffer framebuffer, RenderLayerGuard guard, CancellationToken cancellationToken)
        {
            lock (renderer.deviceSystem.GetCommandPoolLock(CommandPoolSelector.Background))
            {
                commandBuffer.Reset();
                commandBuffer.Begin(CommandBufferUsageFlags.SimultaneousUse);
                commandBuffer.BeginRenderPass(RenderPass.RenderPass,
                    framebuffer,
                    renderer.canvas.Rect,
                    RenderPass.ClearValues,
                    SubpassContents.Inline);
                commandBuffer.BindPipeline(PipelineBindPoint.Graphics, GraphicsPipeline.Pipeline);
                guard.Pipeline = GraphicsPipeline;
                guard.Buffer = commandBuffer;
                AjivaLayerRenderSystem.DrawComponents(guard, cancellationToken);
                if (cancellationToken.IsCancellationRequested) return;
                commandBuffer.EndRenderPass();
                commandBuffer.End();
            }
        }
    }
}
