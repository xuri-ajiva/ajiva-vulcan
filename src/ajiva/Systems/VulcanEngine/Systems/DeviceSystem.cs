using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using ajiva.Systems.VulcanEngine.Interfaces;
using SharpVk;
using SharpVk.Khronos;

namespace ajiva.Systems.VulcanEngine.Systems;

[Dependent(typeof(WindowSystem))]
public class DeviceSystem : SystemBase, IDeviceSystem
{
    private readonly WindowSystem _windowSystem;
    private QueueFamilyIndices queueFamilies;

    /// <inheritdoc />
    public DeviceSystem(Instance instance, WindowSystem windowSystem)
    {
        _windowSystem = windowSystem;
        PickPhysicalDevice(instance);
        CreateLogicalDevice();
    }

    public PhysicalDevice? PhysicalDevice { get; private set; }
    public Device? Device { get; private set; }

    internal Queue? GraphicsQueue { get; private set; }
    internal Queue? PresentQueue { get; private set; }
    internal Queue? TransferQueue { get; private set; }

    internal ConcurrentQueue<Action<CommandBuffer>> GraphicsQueueQueue { get; } = new ConcurrentQueue<Action<CommandBuffer>>();
    internal ConcurrentQueue<Action<CommandBuffer>> PresentQueueQueue { get; } = new ConcurrentQueue<Action<CommandBuffer>>();
    internal ConcurrentQueue<Action<CommandBuffer>> TransferQueueQueue { get; } = new ConcurrentQueue<Action<CommandBuffer>>();

    public Fence TransferQueueFence { get; private set; }
    public Fence PresentQueueFence { get; private set; }
    public Fence GraphicsQueueFence { get; private set; }

    public CommandPool? TransientCommandPool { get; private set; }
    private CommandPool? BackgroundCommandPool { get; set; }
    private CommandPool? ForegroundCommandPool { get; set; }

    private object TransientCommandPoolLock { get; } = new object();
    private object BackgroundCommandPoolLock { get; } = new object();
    private object ForegroundCommandPoolLock { get; } = new object();

    public CommandBuffer? TransientSingleCommandBuffer { get; private set; }
    public CommandBuffer? BackgroundSingleCommandBuffer { get; private set; }
    public CommandBuffer? ForegroundSingleCommandBuffer { get; private set; }

    private List<IDisposable> Disposables { get; set; } = new List<IDisposable>();

    private void PickPhysicalDevice(Instance instance)
    {
        var availableDevices = instance.EnumeratePhysicalDevices();

        PhysicalDevice = availableDevices.First(x => x.IsSuitableDevice(_windowSystem.Canvas));
    }

    private void CreateLogicalDevice()
    {
        queueFamilies = PhysicalDevice!.FindQueueFamilies(_windowSystem.Canvas);

        Device = PhysicalDevice!.CreateDevice(queueFamilies.Indices
                .Select(index => new DeviceQueueCreateInfo {
                    QueueFamilyIndex = index,
                    QueuePriorities = new[] {
                        1f
                    }
                }).ToArray(),
            null,
            KhrExtensions.Swapchain, DeviceCreateFlags.None, new PhysicalDeviceFeatures {
                //TODO Enable Features Used in any other part // remainder
                SamplerAnisotropy = true,
                FillModeNonSolid = true,
                SampleRateShading = true,
            });

        GraphicsQueue = Device.GetQueue(queueFamilies.GraphicsFamily!.Value, 0);
        PresentQueue = Device.GetQueue(queueFamilies.PresentFamily!.Value, 0);
        TransferQueue = Device.GetQueue(queueFamilies.TransferFamily!.Value, 0);

        GraphicsQueueFence = Device.CreateFence();
        PresentQueueFence = Device.CreateFence();
        TransferQueueFence = Device.CreateFence();
    }

    public void WaitIdle()
    {
        lock (Device) // TODO: is needed, but where are more places to lock the device?
        {
            Device?.WaitIdle();
        }
    }

#region BufferAndMemory

    public uint FindMemoryType(uint typeFilter, MemoryPropertyFlags flags)
    {
        var memoryProperties = PhysicalDevice!.GetMemoryProperties();

        for (var i = 0; i < memoryProperties.MemoryTypes.Length; i++)
            if ((typeFilter & (1u << i)) > 0
                && memoryProperties.MemoryTypes[i].PropertyFlags.HasFlag(flags))
                return (uint)i;

        throw new Exception("No compatible memory type.");
    }

#endregion

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        foreach (var buffer in Disposables) buffer.Dispose();
        Disposables.Clear();
        Disposables = null!;

        TransferQueueFence.Dispose();
        PresentQueueFence.Dispose();
        GraphicsQueueFence.Dispose();

        TransientCommandPool?.FreeCommandBuffers(TransientSingleCommandBuffer);
        TransientCommandPool?.Dispose();
        ForegroundCommandPool?.FreeCommandBuffers(ForegroundSingleCommandBuffer);
        ForegroundCommandPool?.Dispose();
        BackgroundCommandPool?.FreeCommandBuffers(BackgroundSingleCommandBuffer);
        BackgroundCommandPool?.Dispose();
        Device?.Dispose();

        TransientSingleCommandBuffer = null;
        ForegroundSingleCommandBuffer = null;
        BackgroundSingleCommandBuffer = null;
        ForegroundCommandPool = null;
        BackgroundCommandPool = null;
        TransientCommandPool = null;
        Device = null;
        PhysicalDevice = null;
    }

    public void WatchObject(IDisposable disposable)
    {
        Disposables.Add(disposable);
    }

    public CommandBuffer[] AllocateCommandBuffers(CommandBufferLevel bufferLevel, int count, CommandPoolSelector selector)
    {
        lock (GetCommandPoolLock(selector))
        {
            System.Diagnostics.Debug.Assert(Device != null, nameof(Device) + " != null");
            return Device.AllocateCommandBuffers(GetCommandPool(selector), bufferLevel, (uint)count);
        }
    }

    public CommandBuffer AllocateCommandBuffer(CommandBufferLevel bufferLevel, CommandPoolSelector selector)
    {
        lock (GetCommandPoolLock(selector))
        {
            System.Diagnostics.Debug.Assert(Device != null, nameof(Device) + " != null");
            return Device.AllocateCommandBuffers(GetCommandPool(selector), bufferLevel, 1)[0];
        }
    }

#region CommandPool

    public void UseCommandPool(Action<CommandPool> action, CommandPoolSelector selector)
    {
        EnsureCommandPoolsExists();

        lock (GetCommandPoolLock(selector))
        {
            action?.Invoke(GetCommandPool(selector));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private CommandPool GetCommandPool(CommandPoolSelector selector)
    {
        return selector switch {
            CommandPoolSelector.Foreground => ForegroundCommandPool!,
            CommandPoolSelector.Background => BackgroundCommandPool!,
            CommandPoolSelector.Transit => TransientCommandPool!,
            _ => throw new ArgumentOutOfRangeException(nameof(selector), selector, null)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private CommandBuffer GetSingleCommandBuffer(CommandPoolSelector selector)
    {
        return selector switch {
            CommandPoolSelector.Foreground => ForegroundSingleCommandBuffer!,
            CommandPoolSelector.Background => BackgroundSingleCommandBuffer!,
            CommandPoolSelector.Transit => TransientSingleCommandBuffer!,
            _ => throw new ArgumentOutOfRangeException(nameof(selector), selector, null)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public object GetCommandPoolLock(CommandPoolSelector selector)
    {
        return selector switch {
            CommandPoolSelector.Foreground => ForegroundCommandPoolLock,
            CommandPoolSelector.Background => BackgroundCommandPoolLock,
            CommandPoolSelector.Transit => TransientCommandPoolLock,
            _ => throw new ArgumentOutOfRangeException(nameof(selector), selector, null)
        };
    }

    private void EnsureCommandPoolsExists()
    {
        TransientCommandPool ??= Device!.CreateCommandPool(queueFamilies.TransferFamily!.Value, CommandPoolCreateFlags.Transient | CommandPoolCreateFlags.ResetCommandBuffer);

        BackgroundCommandPool ??= Device!.CreateCommandPool(queueFamilies.GraphicsFamily!.Value, CommandPoolCreateFlags.ResetCommandBuffer);
        ForegroundCommandPool ??= Device!.CreateCommandPool(queueFamilies.GraphicsFamily!.Value, CommandPoolCreateFlags.ResetCommandBuffer);

        TransientSingleCommandBuffer ??= Device!.AllocateCommandBuffers(TransientCommandPool, CommandBufferLevel.Primary, 1).Single();
        BackgroundSingleCommandBuffer ??= Device!.AllocateCommandBuffers(BackgroundCommandPool, CommandBufferLevel.Primary, 1).Single();
        ForegroundSingleCommandBuffer ??= Device!.AllocateCommandBuffers(ForegroundCommandPool, CommandBufferLevel.Primary, 1).Single();
    }

    public void ExecuteSingleTimeCommand(QueueType queueType, CommandPoolSelector poolSelector, Action<CommandBuffer>? action)
    {
        EnsureCommandPoolsExists();

        GetQueueByType(queueType, poolSelector, out var queue, out var fence, out var queueQueue, out var commandBuffer, out var poolLock);
        lock (queue)
        lock (poolLock)
        {
            ExecuteOnQueueWithFence(action, queue, fence, poolLock, commandBuffer, queueType);
        }
    }

    public void QueueSingleTimeCommand(QueueType queueType, CommandPoolSelector poolSelector, Action<CommandBuffer> action)
    {
        GetQueueByType(queueType, poolSelector, out var queue, out var fence, out var queueQueue, out var commandBuffer, out var poolLock);
        lock (queueQueue)
        {
            queueQueue.Enqueue(action);
        }
    }

    public void ExecuteSingleTimeCommands(QueueType queueType, CommandPoolSelector poolSelector)
    {
        EnsureCommandPoolsExists();

        GetQueueByType(queueType, poolSelector, out var queue, out var fence, out var queueQueue, out var commandBuffer, out var poolLock);

        lock (commandBuffer)
        {
            if (queue is null)
                throw new Exception("Init not done!");

            lock (queue)
            {
                while (queueQueue.TryDequeue(out var action))
                {
                    if (fence.GetStatus() == Result.Success)
                    {
                        ALog.Error("Fence Error");
                    }

                    ExecuteOnQueueWithFence(action, queue, fence, poolLock, commandBuffer, queueType);
                }
            }
        }
    }

    private void ExecuteOnQueueWithFence(Action<CommandBuffer>? action, Queue queue, Fence fence, object commandPoolLock, CommandBuffer singleCommandBuffer, QueueType type)
    {
        if (action is null)
        {
            ALog.Error("Action is Null");
            return;
        }

        lock (commandPoolLock)
        {
            singleCommandBuffer.Begin(CommandBufferUsageFlags.OneTimeSubmit);

            action.Invoke(singleCommandBuffer);

            singleCommandBuffer.End();
        }

        queue.Submit(new SubmitInfo
        {
            CommandBuffers = new[]
            {
                singleCommandBuffer
            }
        }, fence);

        queue.WaitIdle();
        fence.Wait(DEFAULT_TIMEOUT);
        fence.Reset();

        if (type != QueueType.GraphicsQueue) return;

        lock (commandPoolLock)
        {
            singleCommandBuffer.Reset(CommandBufferResetFlags.ReleaseResources);
        }
    }

    private void GetQueueByType(QueueType queueType, CommandPoolSelector commandPoolSelector, out Queue queue, out Fence fence, out ConcurrentQueue<Action<CommandBuffer>> queueQueue, out CommandBuffer commandBuffer, out object poolLock)
    {
        commandBuffer = GetSingleCommandBuffer(commandPoolSelector);
        poolLock = GetCommandPoolLock(commandPoolSelector);
        switch (queueType)
        {
            case QueueType.GraphicsQueue:
                queue = GraphicsQueue!;
                queueQueue = GraphicsQueueQueue;
                fence = GraphicsQueueFence;
                break;
            case QueueType.PresentQueue:
                queue = PresentQueue!;
                queueQueue = PresentQueueQueue;
                fence = PresentQueueFence;
                break;
            case QueueType.TransferQueue:
                queueQueue = TransferQueueQueue;
                queue = TransferQueue!;
                fence = TransferQueueFence;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(queueType), queueType, null);
        }
    }

    private const ulong DEFAULT_TIMEOUT = 10_000_000UL; // 10 ms in ns

#endregion
}
public enum CommandPoolSelector
{
    Foreground,
    Background,
    Transit
}
public enum QueueType
{
    GraphicsQueue,
    PresentQueue,
    TransferQueue
}
