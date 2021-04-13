using System;
using System.Linq;
using ajiva.Ecs;
using ajiva.Ecs.System;
using ajiva.Ecs.Utils;
using ajiva.Models;
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

        public CommandBuffer? SingleCommandBuffer { get; private set; }

        private QueueFamilyIndices queueFamilies;

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

        internal CommandPool? TransientCommandPool;
        public CommandPool? CommandPool;

        private void EnsureCommandPoolsExists()
        {
            TransientCommandPool ??= Device!.CreateCommandPool(queueFamilies.TransferFamily!.Value, CommandPoolCreateFlags.Transient);

            CommandPool ??= Device!.CreateCommandPool(queueFamilies.GraphicsFamily!.Value, CommandPoolCreateFlags.ResetCommandBuffer);

            SingleCommandBuffer ??= Device!.AllocateCommandBuffers(CommandPool, CommandBufferLevel.Primary, 1).Single();
        }

        public void SingleTimeCommand(Func<DeviceSystem, Queue> queueSelector, Action<CommandBuffer> action)
        {
            EnsureCommandPoolsExists();

            lock (SingleCommandBuffer!)
            {
                SingleCommandBuffer.Begin(CommandBufferUsageFlags.OneTimeSubmit);

                action.Invoke(SingleCommandBuffer);

                SingleCommandBuffer.End();

                var queue = queueSelector(this);

                queue.Submit(new SubmitInfo
                {
                    CommandBuffers = new[]
                    {
                        SingleCommandBuffer
                    },
                }, null);

                queue.WaitIdle();
                SingleCommandBuffer.Reset(CommandBufferResetFlags.ReleaseResources);
            }
        }

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
        public void Init(AjivaEcs ecs)
        {
            PickPhysicalDevice(ecs.GetInstance<Instance>());
            CreateLogicalDevice();
        }

        /// <inheritdoc />
        public DeviceSystem(AjivaEcs ecs) : base(ecs)
        {
        }
    }
}
