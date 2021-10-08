using System;
using System.Collections.Generic;
using System.Linq;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Ecs.System;
using ajiva.Ecs.Utils;
using ajiva.Models;
using Ajiva.Wrapper.Logger;
using SharpVk;
using SharpVk.Khronos;

namespace ajiva.Systems.VulcanEngine.Systems
{
    [Dependent(typeof(WindowSystem))]
    public class DeviceSystem : SystemBase, IInit
    {
        internal PhysicalDevice? PhysicalDevice { get; private set; }
        internal Device? Device { get; private set; }

        internal Queue? GraphicsQueue { get; private set; }
        internal Queue? PresentQueue { get; private set; }
        internal Queue? TransferQueue { get; private set; }

        internal Queue<Action<CommandBuffer>> GraphicsQueueQueue { get; private set; } = new();
        internal Queue<Action<CommandBuffer>> PresentQueueQueue { get; private set; } = new();
        internal Queue<Action<CommandBuffer>> TransferQueueQueue { get; private set; } = new();

        public Fence TransferQueueFence { get; private set; }
        public Fence PresentQueueFence { get; private set; }
        public Fence GraphicsQueueFence { get; private set; }

        public CommandBuffer? SingleCommandBuffer { get; private set; }
        public CommandPool? TransientCommandPool { get; private set; }
        private CommandPool? CommandPool { get; set; }

        private QueueFamilyIndices queueFamilies;
        private readonly object commandPoolLock = new();

        private void PickPhysicalDevice(Instance instance)
        {
            var availableDevices = instance.EnumeratePhysicalDevices();

            PhysicalDevice = availableDevices.First(x => x.IsSuitableDevice(Ecs.GetSystem<WindowSystem>().Canvas));
        }

        private void CreateLogicalDevice()
        {
            queueFamilies = PhysicalDevice!.FindQueueFamilies(Ecs.GetSystem<WindowSystem>().Canvas);

            Device = PhysicalDevice!.CreateDevice(queueFamilies.Indices
                    .Select(index => new DeviceQueueCreateInfo
                    {
                        QueueFamilyIndex = index,
                        QueuePriorities = new[]
                        {
                            1f
                        },
                    }).ToArray(),
                null,
                KhrExtensions.Swapchain, DeviceCreateFlags.None, new PhysicalDeviceFeatures()
                {
                    SamplerAnisotropy = true,
                    FillModeNonSolid = true,
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
            Device?.WaitIdle();
        }

#region BufferAndMemory

        public uint FindMemoryType(uint typeFilter, MemoryPropertyFlags flags)
        {
            var memoryProperties = PhysicalDevice!.GetMemoryProperties();

            for (var i = 0; i < memoryProperties.MemoryTypes.Length; i++)
            {
                if ((typeFilter & (1u << i)) > 0
                    && memoryProperties.MemoryTypes[i].PropertyFlags.HasFlag(flags))
                {
                    return (uint)i;
                }
            }

            throw new("No compatible memory type.");
        }

#endregion

#region CommandPool

        public void UseCommandPool(Action<CommandPool> action)
        {
            lock (commandPoolLock)
            {
                action?.Invoke(CommandPool);
            }
        }

        private void EnsureCommandPoolsExists()
        {
            TransientCommandPool ??= Device!.CreateCommandPool(queueFamilies.TransferFamily!.Value, CommandPoolCreateFlags.Transient);

            CommandPool ??= Device!.CreateCommandPool(queueFamilies.GraphicsFamily!.Value, CommandPoolCreateFlags.ResetCommandBuffer);

            SingleCommandBuffer ??= Device!.AllocateCommandBuffers(CommandPool, CommandBufferLevel.Primary, 1).Single();
        }

        public void ExecuteSingleTimeCommand(QueueType queueType, Action<CommandBuffer> action)
        {
            EnsureCommandPoolsExists();

            System.Diagnostics.Debug.Assert(SingleCommandBuffer != null, nameof(SingleCommandBuffer) + " != null");

            GetQueusByType(queueType, out var queue, out Fence fence, out Queue<Action<CommandBuffer>> queueQueue);
            lock (SingleCommandBuffer)
            lock (queue)
                ExecuteOnQueueWithFence(action, queue, fence);
        }

        public void QueueSingleTimeCommand(QueueType queueType, Action<CommandBuffer> action)
        {
            GetQueusByType(queueType, out _, out _, out Queue<Action<CommandBuffer>> queueQueue);
            queueQueue.Enqueue(action);
        }

        public void ExecuteSingleTimeCommands(QueueType queueType)
        {
            EnsureCommandPoolsExists();
            System.Diagnostics.Debug.Assert(SingleCommandBuffer != null, nameof(SingleCommandBuffer) + " != null");

            GetQueusByType(queueType, out var queue, out Fence fence, out Queue<Action<CommandBuffer>> queueQueue);

            lock (SingleCommandBuffer)
            {
                if (queue is null)
                    throw new("Init not done!");

                lock (queue)
                {
                    while (queueQueue.Count != 0)
                    {
                        if (fence.GetStatus() == Result.Success)
                        {
                            LogHelper.Log("Fence Error");
                        }

                        var action = queueQueue.Dequeue();

                        ExecuteOnQueueWithFence(action, queue, fence);
                    }
                }
            }
        }

        private void ExecuteOnQueueWithFence(Action<CommandBuffer> action, Queue queue, Fence fence)
        {
            System.Diagnostics.Debug.Assert(SingleCommandBuffer != null, nameof(SingleCommandBuffer) + " != null");
            SingleCommandBuffer.Begin(CommandBufferUsageFlags.OneTimeSubmit);

            action.Invoke(SingleCommandBuffer);

            SingleCommandBuffer.End();

            queue.Submit(new SubmitInfo
            {
                CommandBuffers = new[]
                {
                    SingleCommandBuffer
                },
            }, fence);

            queue.WaitIdle();
            fence.Wait(DEFAULT_TIMEOUT);
            fence.Reset();
            SingleCommandBuffer.Reset(CommandBufferResetFlags.ReleaseResources);
        }

        private void GetQueusByType(QueueType queueType, out Queue queue, out Fence fence, out Queue<Action<CommandBuffer>> queueQueue)
        {
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

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            TransientCommandPool?.Dispose();
            CommandPool?.FreeCommandBuffers(SingleCommandBuffer);
            CommandPool?.Dispose();
            Device?.Dispose();

            SingleCommandBuffer = null;
            CommandPool = null;
            TransientCommandPool = null;
            Device = null;
            PhysicalDevice = null;
        }

        /// <inheritdoc />
        public void Init()
        {
            PickPhysicalDevice(Ecs.GetInstance<Instance>());
            CreateLogicalDevice();
        }

        /// <inheritdoc />
        public DeviceSystem(IAjivaEcs ecs) : base(ecs)
        {
        }
    }

    public enum QueueType
    {
        GraphicsQueue,
        PresentQueue,
        TransferQueue,
    }
}
