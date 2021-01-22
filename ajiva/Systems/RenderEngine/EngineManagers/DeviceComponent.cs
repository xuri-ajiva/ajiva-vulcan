using System;
using System.Linq;
using System.Runtime.CompilerServices;
using ajiva.Helpers;
using ajiva.Models;
using ajiva.Systems.RenderEngine.Engine;
using SharpVk;
using SharpVk.Khronos;

namespace ajiva.Systems.RenderEngine.EngineManagers
{
    public class DeviceComponent : RenderEngineComponent
    {
        internal PhysicalDevice? PhysicalDevice { get; private set; }
        internal Device? Device { get; private set; }

        internal Queue? GraphicsQueue { get; private set; }
        internal Queue? PresentQueue { get; private set; }
        internal Queue? TransferQueue { get; private set; }

        public DeviceComponent(IRenderEngine renderEngine) : base(renderEngine)
        {
        }

        public void EnsureDevicesExist()
        {
            RenderEngine.Window.EnsureSurfaceExists();
            if (RenderEngine.DeviceComponent.PhysicalDevice == null) PickPhysicalDevice();
            if (RenderEngine.DeviceComponent.Device == null) CreateLogicalDevice();
        }

        private void PickPhysicalDevice()
        {
            ATrace.Assert(RenderEngine.Instance != null, "renderEngine.Instance != null");
            var availableDevices = RenderEngine.Instance.EnumeratePhysicalDevices();

            PhysicalDevice = availableDevices.First(IsSuitableDevice);
        }

        private void CreateLogicalDevice()
        {
            var queueFamilies = FindQueueFamilies(PhysicalDevice);

            Device = PhysicalDevice.CreateDevice(queueFamilies.Indices
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

        public QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
        {
            var indices = new QueueFamilyIndices();

            var queueFamilies = device.GetQueueFamilyProperties();

            for (uint index = 0; index < queueFamilies.Length && !indices.IsComplete; index++)
            {
                if (queueFamilies[index].QueueFlags.HasFlag(QueueFlags.Graphics))
                {
                    indices.GraphicsFamily = index;
                }

                if (device.GetSurfaceSupport(index, RenderEngine.Window.Surface))
                {
                    indices.PresentFamily = index;
                }

                if (queueFamilies[index].QueueFlags.HasFlag(QueueFlags.Transfer) && !queueFamilies[index].QueueFlags.HasFlag(QueueFlags.Graphics))
                {
                    indices.TransferFamily = index;
                }
            }

            indices.TransferFamily ??= indices.GraphicsFamily;

            return indices;
        }

        private bool IsSuitableDevice(PhysicalDevice dvc)
        {
            var features = dvc.GetFeatures();

            return dvc.EnumerateDeviceExtensionProperties(null).Any(extension => extension.ExtensionName == KhrExtensions.Swapchain)
                   && FindQueueFamilies(dvc).IsComplete && features.SamplerAnisotropy;
        }

        public void WaitIdle()
        {
            Device?.WaitIdle();
        }

        public void Submit(CommandBuffer[] commandBuffers, PipelineStageFlags[] waitDestinationStageMask)
        {
            GraphicsQueue.Submit(new SubmitInfo
            {
                CommandBuffers = commandBuffers,
                SignalSemaphores = new[]
                {
                    RenderEngine.SemaphoreComponent.RenderFinished
                },
                WaitDestinationStageMask = waitDestinationStageMask,
                WaitSemaphores = new[]
                {
                    RenderEngine.SemaphoreComponent.ImageAvailable
                }
            }, null);
        }

        public void Present(in uint nextImage)
        {
            PresentQueue.Present(RenderEngine.SemaphoreComponent.RenderFinished, RenderEngine.SwapChainComponent.SwapChain, nextImage, new Result[1]);
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

        public void EnsureCommandPoolsExists()
        {
            EnsureDevicesExist();

            var queueFamilies = FindQueueFamilies(PhysicalDevice!);

            TransientCommandPool ??= Device!.CreateCommandPool(queueFamilies.TransferFamily!.Value, CommandPoolCreateFlags.Transient);

            CommandPool ??= Device!.CreateCommandPool(queueFamilies.GraphicsFamily!.Value);
        }

        

        public void SingleTimeCommand(Func<DeviceComponent, Queue> queueSelector, Action<CommandBuffer> action)
        {
            EnsureCommandPoolsExists();

            var commandBuffer = Device!.AllocateCommandBuffer(CommandPool, CommandBufferLevel.Primary);
            commandBuffer.Begin(CommandBufferUsageFlags.OneTimeSubmit);

            action.Invoke(commandBuffer);

            commandBuffer.End();

            var queue = queueSelector(this);

            queue.Submit(new SubmitInfo()
            {
                CommandBuffers = new[]
                {
                    commandBuffer
                },
            }, null);

            queue.WaitIdle();
            CommandPool!.FreeCommandBuffers(commandBuffer);
            commandBuffer = null;
        }

  #endregion
        
        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            TransientCommandPool?.Dispose();
            CommandPool?.Dispose();
            Device?.Dispose();
        }
    }
}
