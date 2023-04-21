using Ajiva.Systems.VulcanEngine.Layer;
using Ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace Ajiva.Systems.VulcanEngine.Layers;

public class CommandBufferPool
{
    private readonly DeviceSystem deviceSystem;

    private readonly List<RenderBuffer> allocatedBuffers = new List<RenderBuffer>();
    private readonly Queue<RenderBuffer> availableBuffers = new Queue<RenderBuffer>();
    private readonly Dictionary<CommandBuffer, RenderBuffer> renderBuffersLockup = new Dictionary<CommandBuffer, RenderBuffer>();

    public CommandBufferPool(DeviceSystem deviceSystem)
    {
        this.deviceSystem = deviceSystem;
    }

    public static void BeginRecordeRenderBuffer(CommandBuffer commandBuffer, FrameViewPortInfo framebuffer, RenderLayerGuard guard, CancellationToken cancellationToken)
    {
        commandBuffer.Reset();
        commandBuffer.Begin(CommandBufferUsageFlags.SimultaneousUse);
        commandBuffer.BeginRenderPass(guard.RenderPassInfo.RenderPass,
            framebuffer.Framebuffer,
            framebuffer.FullRec,
            guard.ClearValues,
            SubpassContents.Inline);
        commandBuffer.SetViewport(0, framebuffer.Viewport);
        commandBuffer.SetScissor(0, framebuffer.FullRec);
        commandBuffer.BindPipeline(PipelineBindPoint.Graphics, guard.Pipeline.Pipeline);
    }
    
    public static void EndRecordeRenderBuffer(CommandBuffer commandBuffer)
    {
        commandBuffer.EndRenderPass();
        commandBuffer.End();
    }


    public RenderBuffer GetNewBuffer()
    {
        return GetNextBuffer();
    }

    private void AllocateNewBuffers()
    {
        var renderBuffer = new RenderBuffer(deviceSystem.AllocateCommandBuffer(CommandBufferLevel.Primary, CommandPoolSelector.Background), -1);
        allocatedBuffers.Add(renderBuffer);
        renderBuffersLockup.Add(renderBuffer.CommandBuffer, renderBuffer);
        availableBuffers.Enqueue(renderBuffer);
        if (allocatedBuffers.Count > 50)
            Log.Warning("Alloc Buffer for CommandBuffer@{GetHashCode()}, Total Buffers: {Count}",GetHashCode(),allocatedBuffers.Count);
    }

    private RenderBuffer GetNextBuffer()
    {
        if (!availableBuffers.Any()) AllocateNewBuffers();
        availableBuffers.TryDequeue(out var result);
        System.Diagnostics.Debug.Assert(result is not null, nameof(result) + " != null");
        System.Diagnostics.Debug.Assert(result.Captured.Count == 0, "result.Captured.Count == 0");
        return result;
    }

    private void Reset(RenderBuffer renderBuffer)
    {
        renderBuffer.Captured.Clear();
        renderBuffer.InUse = false;
        availableBuffers.Enqueue(renderBuffer);
    }

    public void ReturnBuffer(RenderBuffer? renderBuffer)
    {
        if (renderBuffer is null) return;
        Reset(renderBuffer);
    }

    public void ReturnBuffer(CommandBuffer? commandBuffer)
    {
        if (commandBuffer is null) return;
        ReturnBuffer(renderBuffersLockup[commandBuffer]);
    }

    private void FreeBuffers(RenderBuffer? renderBuffer)
    {
        if (renderBuffer is null) return;
        allocatedBuffers.Remove(renderBuffer);
        if (availableBuffers.Contains(renderBuffer)) Log.Error("Buffer Available but should be deleted!");
        deviceSystem.UseCommandPool(x =>
        {
            x.FreeCommandBuffers(renderBuffer.CommandBuffer);
        }, CommandPoolSelector.Background);
    }

    private void FreeBuffers(CommandBuffer? commandBuffer)
    {
        if (commandBuffer is null) return;
        var renderBuffer = renderBuffersLockup[commandBuffer];
        FreeBuffers(renderBuffer);
    }
}
