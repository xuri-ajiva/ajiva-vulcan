using System;
using System.Linq;
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

        public void SingleTimeCommand(QueueType queueType, Action<CommandBuffer> action)
        {
            EnsureCommandPoolsExists();

            lock (SingleCommandBuffer!)
            {
                SingleCommandBuffer.Begin(CommandBufferUsageFlags.OneTimeSubmit);

                action.Invoke(SingleCommandBuffer);

                SingleCommandBuffer.End();

                var queue = queueType switch
                {
                    QueueType.GraphicsQueue => GraphicsQueue,
                    QueueType.PresentQueue => PresentQueue,
                    QueueType.TransferQueue => TransferQueue,
                    _ => throw new ArgumentOutOfRangeException(nameof(queueType), queueType, null)
                };
                var fence = queueType switch
                {
                    QueueType.GraphicsQueue => GraphicsQueueFence,
                    QueueType.PresentQueue => PresentQueueFence,
                    QueueType.TransferQueue => TransferQueueFence,
                    _ => throw new ArgumentOutOfRangeException(nameof(queueType), queueType, null)
                };

                if (queue is null)
                    throw new("Init not done!");
                lock (queue)
                {
                    if (fence.GetStatus() == Result.Success)
                    {
                        LogHelper.Log("Fence Error");
                    }

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
                }
                SingleCommandBuffer.Reset(CommandBufferResetFlags.ReleaseResources);
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
        public void Init(IAjivaEcs ecs)
        {
            PickPhysicalDevice(ecs.GetInstance<Instance>());
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
