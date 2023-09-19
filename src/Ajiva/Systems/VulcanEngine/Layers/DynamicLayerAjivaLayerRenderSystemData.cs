/*
using System.Diagnostics.CodeAnalysis;
using Ajiva.Systems.VulcanEngine.Layer;
using Ajiva.Systems.VulcanEngine.Layers.Models;
using Ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace Ajiva.Systems.VulcanEngine.Layers;


public class DynamicLayerAjivaLayerRenderSystemData : DisposingLogger
{
    private const int QueueMembersMax = 2;
    private readonly IAjivaLayerRenderSystem AjivaLayerRenderSystem;

    private readonly List<RenderBuffer> allocatedBuffers = new List<RenderBuffer>();
    private readonly Queue<RenderBuffer> availableBuffers = new Queue<RenderBuffer>();
    private readonly IAjivaLayer AjivaLayer;

    public readonly object Lock = new object();
    private readonly object lockForFillBuffer = new object();
    private readonly object lockForFillBufferQueue = new object();
    private readonly Dictionary<CommandBuffer, RenderBuffer> renderBuffersLockup = new Dictionary<CommandBuffer, RenderBuffer>();
    private readonly AjivaLayerRenderer renderer;

    private readonly object upToDateLock = new object();
    private int queueForFillBuffer;

    public DynamicLayerAjivaLayerRenderSystemData(AjivaLayerRenderer renderer,
        IAjivaLayer AjivaLayer,
        IAjivaLayerRenderSystem AjivaLayerRenderSystem)
    {
        this.renderer = renderer;
        this.AjivaLayer = AjivaLayer;
        this.AjivaLayerRenderSystem = AjivaLayerRenderSystem;
        for (var i = 0; i < Const.Default.BackupBuffers; i++) AllocateNewBuffers();
    }

    public CancellationTokenSource TokenSource { get; } = new CancellationTokenSource();

    public long CurrentActiveVersion { get; private set; }

    public RenderBuffer? UpToDateBuffer { get; private set; }

#region BufferManagment

    public void FillNextBufferBlockingUnchecked(CancellationToken cancellationToken)
    {
        FillBuffer(GetNextBuffer(), new RenderLayerGuard(), cancellationToken);
    }

    public void FillNextBufferBlocking(CancellationToken cancellationToken)
    {
        lock (lockForFillBufferQueue)
        {
            if (queueForFillBuffer >= QueueMembersMax)
                //ALog.Debug($"Max Queue Amount for FillNextBuffer of {GetHashCode():X8} reached!");
                return;
            queueForFillBuffer++;
        }
        //waiting threads count max queueMembersMax
        lock (lockForFillBuffer)
        {
            --queueForFillBuffer;
            FillNextBufferBlockingUnchecked(cancellationToken);
        }
    }

    private void AllocateNewBuffers()
    {
        var renderBuffer = new RenderBuffer(renderer.DeviceSystem.AllocateCommandBuffers(CommandBufferLevel.Primary, renderPass.FrameBuffers.Length, CommandPoolSelector.Background), -1);
        allocatedBuffers.Add(renderBuffer);
        foreach (var commandBuffer in renderBuffer.CommandBuffers) renderBuffersLockup.Add(commandBuffer, renderBuffer);
        availableBuffers.Enqueue(renderBuffer);
        if (allocatedBuffers.Count > 20)
            ALog.Warn($"Alloc Buffer for {AjivaLayerRenderSystem}, Total Buffers: {allocatedBuffers.Count}");
    }

    private RenderBuffer GetNextBuffer()
    {
        lock (Lock)
        {
            if (!availableBuffers.Any()) AllocateNewBuffers();
            availableBuffers.TryDequeue(out var result);
            System.Diagnostics.Debug.Assert(result is not null, nameof(result) + " != null");
            System.Diagnostics.Debug.Assert(result.Captured.Count == 0, "result.Captured.Count == 0");
            return result;
        }
    }

    public void FillNextBufferAsync()
    {
        lock (this)
        {
            TaskWatcher.Watch(() =>
            {
                FillNextBufferBlocking(TokenSource.Token);
                return Task.CompletedTask;
            });
        }
    }

    private void FillBuffer(RenderBuffer renderBuffer, RenderLayerGuard guard, CancellationToken cancellationToken)
    {
        lock (renderBuffer)
        {
            var vTmp = AjivaLayerRenderSystem.DataVersion;
            if (renderBuffer.Version == vTmp)
                return;

            guard.RenderBuffer = renderBuffer;
            guard.RenderPassInfo = renderPass;
            guard.Renderer = renderer;
            guard.Pipeline = graphicsPipeline;

            System.Diagnostics.Debug.Assert(renderBuffer.CommandBuffers.Length == renderPass.FrameBuffers.Length, "swapBuffer.Length == RenderPass.FrameBuffers.Length");

            for (var i = 0; i < renderPass.FrameBuffers.Length; i++)
            {
                var framebuffer = renderPass.FrameBuffers[i];

                guard.Buffer = renderBuffer.CommandBuffers[i];
                FillBuffer(framebuffer, guard, cancellationToken);
                if (cancellationToken.IsCancellationRequested) return;
            }

            PushRenderBuffer(renderBuffer, vTmp);
        }
    }

    private void PushRenderBuffer(RenderBuffer renderBuffer, long version)
    {
        lock (Lock)
        {
            renderBuffer.Version = version;
            lock (upToDateLock)
            {
                if (UpToDateBuffer is not null) Reset(UpToDateBuffer);
                UpToDateBuffer = renderBuffer;
            }
            CurrentActiveVersion = version;
        }
    }

    private void Reset(RenderBuffer renderBuffer)
    {
        renderBuffer.Captured.Clear();
        renderBuffer.InUse = false;
        availableBuffers.Enqueue(renderBuffer);
    }

    public bool TryGetUpdatedBuffers([MaybeNullWhen(false)] out RenderBuffer renderBuffer)
    {
        lock (upToDateLock)
        {
            renderBuffer = UpToDateBuffer;
            if (renderBuffer is null) return false;
            renderBuffer.InUse = true;
            UpToDateBuffer = null;
            return true;
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
            var rBuffer = renderBuffersLockup[commandBuffers.First()!];
            Reset(rBuffer);
        }
    }

    private void FreeBuffers(CommandBuffer?[] commandBuffers)
    {
        lock (upToDateLock)
        {
            foreach (var commandBuffer in commandBuffers)
            {
                if (commandBuffer is null) continue;

                var lUp = renderBuffersLockup[commandBuffer];
                allocatedBuffers.Remove(lUp);
                if (availableBuffers.Contains(lUp)) ALog.Error("Buffer Available but should be deleted!");
                renderer.DeviceSystem.UseCommandPool(x =>
                {
                    x.FreeCommandBuffers(commandBuffer);
                }, CommandPoolSelector.Background);
            }
        }
    }

#endregion

    private void FillBuffer(Framebuffer framebuffer, RenderLayerGuard guard, CancellationToken cancellationToken)
    {
        lock (renderer.DeviceSystem.GetCommandPoolLock(CommandPoolSelector.Background))
        {
            BeginRecordeRenderBuffer(guard.Buffer, new FrameViewPortInfo(framebuffer, AjivaLayer.Extent, 0..1), guard, cancellationToken);
            AjivaLayerRenderSystem.DrawComponents(guard, cancellationToken);
            EndRecordeRenderBuffer(guard.Buffer);
        }
    }



    private static void EndRecordeRenderBuffer(CommandBuffer commandBuffer)
    {
        commandBuffer.EndRenderPass();
        commandBuffer.End();
    }


    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        base.ReleaseUnmanagedResources(disposing);

        availableBuffers.Clear();
        TokenSource?.Cancel();

        renderer.DeviceSystem.UseCommandPool(x =>
        {
            foreach (var allocatedBuffer in allocatedBuffers) x.FreeCommandBuffers(allocatedBuffer.CommandBuffers);
        }, CommandPoolSelector.Background);
        allocatedBuffers.Clear();

        graphicsPipeline?.Dispose();
        renderPass.Dispose();
    }

    public void CheckUpToDate()
    {
        if (CurrentActiveVersion != AjivaLayerRenderSystem.DataVersion)
        {
            FillNextBufferAsync();
        }
    }
}
*/
