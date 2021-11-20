using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Interfaces;

public interface IDeviceSystem : ISystem
{
    PhysicalDevice? PhysicalDevice { get; }
    Device? Device { get; }
    Fence TransferQueueFence { get; }
    Fence PresentQueueFence { get; }
    Fence GraphicsQueueFence { get; }
    CommandPool? TransientCommandPool { get; }
    CommandBuffer? TransientSingleCommandBuffer { get; }
    CommandBuffer? BackgroundSingleCommandBuffer { get; }
    CommandBuffer? ForegroundSingleCommandBuffer { get; }
    
    void WaitIdle();
    uint FindMemoryType(uint typeFilter, MemoryPropertyFlags flags);
    void WatchObject(IDisposable disposable);
    CommandBuffer[] AllocateCommandBuffers(CommandBufferLevel bufferLevel, int count, CommandPoolSelector selector);
    CommandBuffer AllocateCommandBuffer(CommandBufferLevel bufferLevel, CommandPoolSelector selector);
    void UseCommandPool(Action<CommandPool> action, CommandPoolSelector selector);
    object GetCommandPoolLock(CommandPoolSelector selector);
    void ExecuteSingleTimeCommand(QueueType queueType, CommandPoolSelector poolSelector, Action<CommandBuffer>? action);
    void QueueSingleTimeCommand(QueueType queueType, CommandPoolSelector poolSelector, Action<CommandBuffer> action);
    void ExecuteSingleTimeCommands(QueueType queueType, CommandPoolSelector poolSelector);
}
