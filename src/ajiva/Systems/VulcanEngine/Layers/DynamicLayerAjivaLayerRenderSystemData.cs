using System.Diagnostics.CodeAnalysis;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Layers;

public class RenderBuffer
{
    public RenderBuffer(CommandBuffer[] commandBuffers, long version)
    {
        CommandBuffers = commandBuffers;
        Version = version;
    }

    public CommandBuffer[] CommandBuffers { get; set; }
    public long Version { get; set; }
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

    private List<CommandBuffer[]> AllocatedBuffers { get; } = new List<CommandBuffer[]>();
    public Queue<RenderBuffer> RenderBuffers { get; } = new Queue<RenderBuffer>();

    public RenderPassLayer RenderPass { get; init; }
    public GraphicsPipelineLayer GraphicsPipeline { get; init; }
    public IAjivaLayerRenderSystem AjivaLayerRenderSystem { get; init; }
    public IAjivaLayer AjivaLayer { get; init; }

    public Task? UpdateTask { get; private set; }
    public CancellationTokenSource TokenSource { get; set; } = new CancellationTokenSource();

    public bool IsBackgroundTaskRunning => UpdateTask is not null;
    public bool IsVersionUpToDate => AjivaLayerRenderSystem.GraphicsDataChanged.Version == CurrentActiveVersion;

    public long CurrentActiveVersion { get; set; }

    public RenderBuffer? UpToDateBuffer { get; set; }

    private void GraphicsDataChangedOnOnChanged(IAjivaLayerRenderSystem sender)
    {
        FillNextBufferAsync();
    }

    public void FillNextBufferBlocking(CancellationToken cancellationToken)
    {
        FillBuffer(GetNextBuffer(), new RenderLayerGuard(), cancellationToken);
    }

    private void AllocateNewBuffers()
    {
        var buffers = new RenderBuffer(renderer.DeviceSystem.AllocateCommandBuffers(CommandBufferLevel.Primary, RenderPass.FrameBuffers.Length, CommandPoolSelector.Background), -1);
        AllocatedBuffers.Add(buffers.CommandBuffers);
        RenderBuffers.Enqueue(buffers);
    }

    private RenderBuffer GetNextBuffer()
    {
        lock (Lock)
        {
            if (!RenderBuffers.Any()) AllocateNewBuffers();
            RenderBuffers.TryDequeue(out var result);
            System.Diagnostics.Debug.Assert(result is not null, nameof(result) + " != null");
            return result;
        }
    }

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

            PushRenderBuffer(vTmp, renderBuffer);
        }
    }

    private void PushRenderBuffer(long version, RenderBuffer renderBuffer)
    {
        lock (Lock)
        {
            renderBuffer.Version = version;
            lock (upToDateLock)
            {
                //todo reuse if not null
                if (UpToDateBuffer is not null) RenderBuffers.Enqueue(UpToDateBuffer);
                UpToDateBuffer = renderBuffer;
            }
            CurrentActiveVersion = version;
        }
    }

    public bool TryGetUpdatedBuffers([MaybeNullWhen(false)] out RenderBuffer renderBuffer)
    {
        lock (upToDateLock)
        {
            renderBuffer = UpToDateBuffer;
            if (renderBuffer is null) return false;
            UpToDateBuffer = null;
            return true;
        }
    }

    public void ReturnBuffer(CommandBuffer?[] commandBuffers)
    {
        if (commandBuffers.Any(x => x is null))
            FreeBuffers(commandBuffers);
        else
            RenderBuffers.Enqueue(new RenderBuffer(commandBuffers, -1));
    }

    private void FreeBuffers(CommandBuffer?[] commandBuffers)
    {
        renderer.DeviceSystem.UseCommandPool(x =>
        {
            x.FreeCommandBuffers(commandBuffers.Where(y => y is not null).ToArray());
        }, CommandPoolSelector.Background);
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

        RenderBuffers.Clear();

        TokenSource?.Cancel();
        UpdateTask?.Dispose();

        renderer.DeviceSystem.UseCommandPool(x =>
        {
            foreach (var allocatedBuffer in AllocatedBuffers) x.FreeCommandBuffers(allocatedBuffer);
        }, CommandPoolSelector.Background);
        AllocatedBuffers.Clear();

        GraphicsPipeline?.Dispose();
        RenderPass.Dispose();
    }
}
