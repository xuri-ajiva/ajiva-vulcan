using System.Diagnostics.CodeAnalysis;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Layers;

public class RenderBuffer
{
    private bool inUse;

    public RenderBuffer(CommandBuffer[] commandBuffers, long version)
    {
        CommandBuffers = commandBuffers;
        Version = version;
    }

    public CommandBuffer[] CommandBuffers { get; set; }
    public long Version { get; set; }

    public List<object> Captured { get; } = new();
    public bool InUse
    {
        get => inUse;
        set
        {
            ALog.Trace($"Set InUse To: {value,6}, {this.GetHashCode():X8}");

            inUse = value;
        }
    }
}
public class DynamicLayerAjivaLayerRenderSystemData : DisposingLogger
{
    private readonly int id;

    public readonly object Lock = new object();
    private readonly AjivaLayerRenderer renderer;

    private readonly object upToDateLock = new object();

    public DynamicLayerAjivaLayerRenderSystemData(
        int id,
        AjivaLayerRenderer renderer,
        RenderPassLayer renderPass,
        GraphicsPipelineLayer graphicsPipeline,
        IAjivaLayer ajivaLayer,
        IAjivaLayerRenderSystem ajivaLayerRenderSystem
    )
    {
        this.id = id;
        this.renderer = renderer;
        RenderPass = renderPass;
        GraphicsPipeline = graphicsPipeline;
        AjivaLayer = ajivaLayer;
        AjivaLayerRenderSystem = ajivaLayerRenderSystem;
        for (var i = 0; i < Const.Default.BackupBuffers; i++) AllocateNewBuffers();

        ajivaLayerRenderSystem.GraphicsDataChanged.OnChanged += GraphicsDataChangedOnOnChanged;
    }

    private List<RenderBuffer> AllocatedBuffers { get; } = new List<RenderBuffer>();
    public Queue<RenderBuffer> AvailableBuffers { get; } = new Queue<RenderBuffer>();
    public Dictionary<CommandBuffer, RenderBuffer> RenderBuffersLockup { get; } = new Dictionary<CommandBuffer, RenderBuffer>();

    public RenderPassLayer RenderPass { get; init; }
    public GraphicsPipelineLayer GraphicsPipeline { get; init; }
    public IAjivaLayerRenderSystem AjivaLayerRenderSystem { get; init; }
    public IAjivaLayer AjivaLayer { get; init; }
    public CancellationTokenSource TokenSource { get; set; } = new CancellationTokenSource();

    public bool IsVersionUpToDate => AjivaLayerRenderSystem.GraphicsDataChanged.Version == CurrentActiveVersion;

    public long CurrentActiveVersion { get; set; }

    public RenderBuffer? UpToDateBuffer { get; set; }
    private readonly object lockForFillBufferQueue = new();
    private readonly object lockForFillBuffer = new();
    private int queueForFillBuffer = 0;
    private const int queueMembersMax = 2;

    private void GraphicsDataChangedOnOnChanged(IAjivaLayerRenderSystem sender)
    {
        FillNextBufferAsync();
    }

    public void FillNextBufferBlockingUnchecked(CancellationToken cancellationToken)
    {
        FillBuffer(GetNextBuffer(), new RenderLayerGuard(), cancellationToken);
    }

    public void FillNextBufferBlocking(CancellationToken cancellationToken)
    {
        lock (lockForFillBufferQueue)
        {
            if (queueForFillBuffer >= queueMembersMax)
            {
                //ALog.Debug($"Max Queue Amount for FillNextBuffer of {GetHashCode():X8} reached!");
                return;
            }
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
        var renderBuffer = new RenderBuffer(renderer.DeviceSystem.AllocateCommandBuffers(CommandBufferLevel.Primary, RenderPass.FrameBuffers.Length, CommandPoolSelector.Background), -1);
        AllocatedBuffers.Add(renderBuffer);
        foreach (var commandBuffer in renderBuffer.CommandBuffers)
        {
            RenderBuffersLockup.Add(commandBuffer, renderBuffer);
        }
        AvailableBuffers.Enqueue(renderBuffer);
        if (AllocatedBuffers.Count > 20)
            ALog.Warn($"Alloc Buffer for {AjivaLayerRenderSystem}, Total Buffers: {AllocatedBuffers.Count}");
    }

    private RenderBuffer GetNextBuffer()
    {
        lock (Lock)
        {
            if (!AvailableBuffers.Any()) AllocateNewBuffers();
            AvailableBuffers.TryDequeue(out var result);
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
            var vTmp = AjivaLayerRenderSystem.GraphicsDataChanged.Version;
            if (renderBuffer.Version == vTmp)
                return;

            guard.RenderBuffer = renderBuffer;

            System.Diagnostics.Debug.Assert(renderBuffer.CommandBuffers.Length == RenderPass.FrameBuffers.Length, "swapBuffer.Length == RenderPass.FrameBuffers.Length");
            lock (AjivaLayerRenderSystem.SnapShotLock)
            {
                AjivaLayerRenderSystem.CreateSnapShot();
                for (var i = 0; i < RenderPass.FrameBuffers.Length; i++)
                {
                    var framebuffer = RenderPass.FrameBuffers[i];

                    FillBuffer(renderBuffer.CommandBuffers[i], framebuffer, guard, cancellationToken);
                    if (cancellationToken.IsCancellationRequested) return;
                }
                AjivaLayerRenderSystem.ClearSnapShot();
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
                if (UpToDateBuffer is not null)
                {
                    Reset(UpToDateBuffer);
                }
                UpToDateBuffer = renderBuffer;
            }
            CurrentActiveVersion = version;
        }
    }

    private void Reset(RenderBuffer renderBuffer)
    {
        renderBuffer.Captured.Clear();
        renderBuffer.InUse = false;
        AvailableBuffers.Enqueue(renderBuffer);
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
            FreeBuffers(commandBuffers);
        else
        {
            var rBuffer = RenderBuffersLockup[commandBuffers.First()!];
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

                var lUp = RenderBuffersLockup[commandBuffer];
                AllocatedBuffers.Remove(lUp);
                if (AvailableBuffers.Contains(lUp))
                {
                    ALog.Error("Buffer Available but should be deleted!");
                }
                renderer.DeviceSystem.UseCommandPool(x =>
                {
                    x.FreeCommandBuffers(commandBuffer);
                }, CommandPoolSelector.Background);
            }
        }
    }

    private void FillBuffer(CommandBuffer commandBuffer, Framebuffer framebuffer, RenderLayerGuard guard, CancellationToken cancellationToken)
    {
        lock (renderer.DeviceSystem.GetCommandPoolLock(CommandPoolSelector.Background))
        {
            commandBuffer.Reset();
            commandBuffer.Begin(CommandBufferUsageFlags.SimultaneousUse);
            commandBuffer.BeginRenderPass(RenderPass.RenderPass,
                framebuffer,
                renderer.Canvas.Rect,
                RenderPass.ClearValues,
                SubpassContents.Inline);
            commandBuffer.SetViewport(0, new Viewport(0, 0, renderer.Canvas.Width, renderer.Canvas.Height, 0, 1));
            commandBuffer.SetScissor(0, renderer.Canvas.Rect);
            commandBuffer.BindPipeline(PipelineBindPoint.Graphics, GraphicsPipeline.Pipeline);
            guard.Pipeline = GraphicsPipeline;
            guard.Buffer = commandBuffer;
            AjivaLayerRenderSystem.DrawComponents(guard, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;
            commandBuffer.EndRenderPass();
            commandBuffer.End();
        }
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        base.ReleaseUnmanagedResources(disposing);

        AjivaLayerRenderSystem.GraphicsDataChanged.OnChanged -= GraphicsDataChangedOnOnChanged;

        AvailableBuffers.Clear();
        TokenSource?.Cancel();

        renderer.DeviceSystem.UseCommandPool(x =>
        {
            foreach (var allocatedBuffer in AllocatedBuffers) x.FreeCommandBuffers(allocatedBuffer.CommandBuffers);
        }, CommandPoolSelector.Background);
        AllocatedBuffers.Clear();

        GraphicsPipeline?.Dispose();
        RenderPass.Dispose();
    }
}
