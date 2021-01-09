using System;
using System.Linq;
using ajiva.Engine;
using ajiva.Models;
using SharpVk;
using SharpVk.Khronos;
using Buffer = SharpVk.Buffer;

namespace ajiva.EngineManagers
{
    public class DeviceManager : IEngineManager
    {
        private readonly IEngine engine;

        internal PhysicalDevice PhysicalDevice { get; private set; }
        internal Device Device { get; private set; }

        internal Queue GraphicsQueue { get; private set; }
        internal Queue PresentQueue { get; private set; }
        internal Queue TransferQueue { get; private set; }

        public DeviceManager(IEngine engine)
        {
            this.engine = engine;
            PhysicalDevice = null!;
            Device = null!;
            GraphicsQueue = null!;
            PresentQueue = null!;
            TransferQueue = null!;
            TransientCommandPool = null!;
            CommandPool = null!;
            CommandBuffers = null!;
        }

        public void CreateDevice()
        {
            PickPhysicalDevice();
            CreateLogicalDevice();
        }

        private void PickPhysicalDevice()
        {
            Throw.Assert(engine.Instance != null, "engine.Instance != null");
            var availableDevices = engine.Instance.EnumeratePhysicalDevices();

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

                if (device.GetSurfaceSupport(index, engine.Window.Surface))
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
            Device.WaitIdle();
        }

        public void Submit(CommandBuffer[] commandBuffers, PipelineStageFlags[] waitDestinationStageMask)
        {
            GraphicsQueue.Submit(new SubmitInfo
            {
                CommandBuffers = commandBuffers,
                SignalSemaphores = new[]
                {
                    engine.SemaphoreManager.RenderFinished
                },
                WaitDestinationStageMask = waitDestinationStageMask,
                WaitSemaphores = new[]
                {
                    engine.SemaphoreManager.ImageAvailable
                }
            }, null);
        }

        public void Present(in uint nextImage)
        {
            PresentQueue.Present(engine.SemaphoreManager.RenderFinished, engine.SwapChainManager.SwapChain, nextImage, new Result[1]);
        }

        #region BufferAndMemory

        public uint FindMemoryType(uint typeFilter, MemoryPropertyFlags flags)
        {
            var memoryProperties = PhysicalDevice.GetMemoryProperties();

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

        internal CommandPool TransientCommandPool;
        public CommandPool CommandPool;
        public CommandBuffer[] CommandBuffers;

        public void CreateCommandPools()
        {
            var queueFamilies = FindQueueFamilies(PhysicalDevice);

            TransientCommandPool = Device.CreateCommandPool(queueFamilies.TransferFamily!.Value, CommandPoolCreateFlags.Transient);

            CommandPool = Device.CreateCommandPool(queueFamilies.GraphicsFamily!.Value);
        }

        public void CreateCommandBuffers()
        {
            CommandPool.Reset(CommandPoolResetFlags.ReleaseResources);

            CommandBuffers = Device.AllocateCommandBuffers(CommandPool, CommandBufferLevel.Primary, (uint)engine.SwapChainManager.FrameBuffers.Length);

            for (var index = 0; index < engine.SwapChainManager.FrameBuffers.Length; index++)
            {
                var commandBuffer = CommandBuffers[index];

                commandBuffer.Begin(CommandBufferUsageFlags.SimultaneousUse);

                commandBuffer.BeginRenderPass(engine.GraphicsManager.RenderPass,
                    engine.SwapChainManager.FrameBuffers[index],
                    new(new(), engine.SwapChainManager.SwapChainExtent),
                    new ClearValue[]
                    {
                        new ClearColorValue(.1f, .1f, .1f, 1), new ClearDepthStencilValue(1, 0)
                    },
                    SubpassContents.Inline);

                commandBuffer.BindPipeline(PipelineBindPoint.Graphics, engine.GraphicsManager.Pipeline);

                engine.BufferManager.BindAllAndDraw(commandBuffer);

                commandBuffer.EndRenderPass();

                commandBuffer.End();
            }
        }

        public void SingleTimeCommand(Func<DeviceManager, Queue> queueSelector, Action<CommandBuffer> action)
        {
            var commandBuffer = Device.AllocateCommandBuffer(CommandPool, CommandBufferLevel.Primary);
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
            CommandPool.FreeCommandBuffers(commandBuffer);
            commandBuffer = null;
        }

        public CommandBuffer BeginSingleTimeCommands()
        {
            var commandBuffer = Device.AllocateCommandBuffers(CommandPool, CommandBufferLevel.Primary, 1);
            commandBuffer[0].Begin(CommandBufferUsageFlags.OneTimeSubmit);

            return commandBuffer[0];
        }

        public void EndSingleTimeCommands(CommandBuffer commandBuffer)
        {
            commandBuffer.End();

            GraphicsQueue.Submit(new SubmitInfo()
            {
                CommandBuffers = new[]
                {
                    commandBuffer
                },
            }, null);

            GraphicsQueue.WaitIdle();
            CommandPool.FreeCommandBuffers(commandBuffer);
        }

  #endregion

        public void FreeCommandBuffers()
        {
            CommandPool?.FreeCommandBuffers(CommandBuffers);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            CommandPool.FreeCommandBuffers(CommandBuffers);
            CommandBuffers = Array.Empty<CommandBuffer>();
            TransientCommandPool.Dispose();
            CommandPool.Dispose();
            Device.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
