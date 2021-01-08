using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        public DeviceManager(IEngine engine)
        {
            this.engine = engine;
        }

        internal PhysicalDevice PhysicalDevice { get; private set; }
        internal Device Device { get; private set; }

        internal Queue GraphicsQueue { get; private set; }
        internal Queue PresentQueue { get; private set; }
        internal Queue TransferQueue { get; private set; }

        /*
                 internal CommandPool transientCommandPool;
        public CommandPool? commandPool;
        public CommandBuffer[]? commandBuffers;
*/

        public void CreateDevice()
        {
            PickPhysicalDevice();
            CreateLogicalDevice();
        }

        private void PickPhysicalDevice()
        {
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
                        }
                    }).ToArray(),
                null,
                KhrExtensions.Swapchain);

            GraphicsQueue = Device.GetQueue(queueFamilies.GraphicsFamily.Value, 0);
            PresentQueue = Device.GetQueue(queueFamilies.PresentFamily.Value, 0);
            TransferQueue = Device.GetQueue(queueFamilies.TransferFamily.Value, 0);
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
            Device?.WaitIdle();
        }

        public struct QueueFamilyIndices
        {
            public uint? GraphicsFamily;
            public uint? PresentFamily;
            public uint? TransferFamily;

            public IEnumerable<uint> Indices
            {
                get
                {
                    if (GraphicsFamily.HasValue)
                    {
                        yield return GraphicsFamily.Value;
                    }

                    if (PresentFamily.HasValue && PresentFamily != GraphicsFamily)
                    {
                        yield return PresentFamily.Value;
                    }

                    if (TransferFamily.HasValue && TransferFamily != PresentFamily && TransferFamily != GraphicsFamily)
                    {
                        yield return TransferFamily.Value;
                    }
                }
            }

            public bool IsComplete =>
                GraphicsFamily.HasValue
                && PresentFamily.HasValue
                && TransferFamily.HasValue;
        }

        public void Dispose()
        {
            Device?.Dispose();
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

        public uint CreateCopyBuffer<T>(ref T[] value, out Buffer stagingBuffer, out DeviceMemory stagingBufferMemory)
        {
            var size = Unsafe.SizeOf<T>();
            var bufferSize = (uint)(size * value.Length);

            CreateBuffer(bufferSize, BufferUsageFlags.TransferSource, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent, out stagingBuffer, out stagingBufferMemory);

            var memoryBuffer = stagingBufferMemory.Map(0, bufferSize, MemoryMapFlags.None);

            for (var index = 0; index < value.Length; index++)
            {
                Marshal.StructureToPtr(value[index], memoryBuffer + (size * index), false);
            }

            stagingBufferMemory.Unmap();
            return bufferSize;
        }

        public void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, out Buffer buffer, out DeviceMemory bufferMemory)
        {
            buffer = Device.CreateBuffer(size, usage, SharingMode.Exclusive, null);

            var memRequirements = buffer.GetMemoryRequirements();

            bufferMemory = Device.AllocateMemory(memRequirements.Size, FindMemoryType(memRequirements.MemoryTypeBits, properties));

            buffer.BindMemory(bufferMemory, 0);
        }

        public void CopyBuffer(Buffer sourceBuffer, Buffer destinationBuffer, ulong size)
        {
            var transferBuffers = Device.AllocateCommandBuffers(TransientCommandPool, CommandBufferLevel.Primary, 1);

            transferBuffers[0].Begin(CommandBufferUsageFlags.OneTimeSubmit);

            transferBuffers[0].CopyBuffer(sourceBuffer, destinationBuffer, new BufferCopy
            {
                Size = size
            });

            transferBuffers[0].End();

            TransferQueue.Submit(new SubmitInfo
            {
                CommandBuffers = transferBuffers
            }, null);
            TransferQueue.WaitIdle();

            TransientCommandPool.FreeCommandBuffers(transferBuffers);
        }

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

        public void CreateVertexBuffers(Vertex[] val, out Buffer vertexBuffer, out DeviceMemory vertexBufferMemory)
        {
            var bufferSize = CreateCopyBuffer(ref val, out var stagingBuffer, out var stagingBufferMemory);

            stagingBufferMemory.Unmap();

            CreateBuffer(bufferSize, BufferUsageFlags.TransferDestination | BufferUsageFlags.VertexBuffer, MemoryPropertyFlags.DeviceLocal, out vertexBuffer, out vertexBufferMemory);

            CopyBuffer(stagingBuffer, vertexBuffer, bufferSize);

            stagingBuffer.Dispose();
            stagingBufferMemory.Free();
        }

        public void CreateIndexBuffer(ushort[] val, out Buffer indexBuffers, out DeviceMemory indexBufferMemory)
        {
            var bufferSize = CreateCopyBuffer(ref val, out var stagingBuffer, out var stagingBufferMemory);

            CreateBuffer(bufferSize, BufferUsageFlags.TransferDestination | BufferUsageFlags.IndexBuffer, MemoryPropertyFlags.DeviceLocal, out indexBuffers, out indexBufferMemory);

            CopyBuffer(stagingBuffer, indexBuffers, bufferSize);

            stagingBuffer.Dispose();
            stagingBufferMemory.Free();
        }

        #endregion

        #region CommandPool

        internal CommandPool TransientCommandPool;
        public CommandPool CommandPool;
        public CommandBuffer[] CommandBuffers;

        public void CreateCommandPools()
        {
            var queueFamilies = FindQueueFamilies(PhysicalDevice);

            TransientCommandPool = Device.CreateCommandPool(queueFamilies.TransferFamily.Value, CommandPoolCreateFlags.Transient);

            CommandPool = Device.CreateCommandPool(queueFamilies.GraphicsFamily.Value);
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

        public void SingleTimeCommand(Action<CommandBuffer> action)
        {
            var commandBuffer = Device.AllocateCommandBuffer(CommandPool, CommandBufferLevel.Primary);
            commandBuffer.Begin(CommandBufferUsageFlags.OneTimeSubmit);

            action.Invoke(commandBuffer);

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
    }
}
