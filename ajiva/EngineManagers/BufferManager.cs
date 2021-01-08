using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ajiva.Engine;
using ajiva.Models;
using SharpVk;
using Buffer = SharpVk.Buffer;

namespace ajiva.EngineManagers
{
    public record BufferPack(Buffer VertexBuffer, DeviceMemory VertexBufferMemory, uint VerticesLength, Buffer IndexBuffer, DeviceMemory IndexBufferMemory, uint IndicesLength);

    public class BufferManager : IDisposable
    {
        public Buffer UniformStagingBuffer;
        public Buffer UniformBuffer;
        public DeviceMemory UniformStagingBufferMemory;
        public DeviceMemory UniformBufferMemory;
        public Program Program { get; private set; }
        private Device Device => Program.DeviceManager.Device;
        private PhysicalDevice PhysicalDevice => Program.DeviceManager.PhysicalDevice;
        private Queue TransferQueue => Program.DeviceManager.TransferQueue;
        private CommandPool TransientCommandPool => Program.DeviceManager.TransientCommandPool;

        public object BufferLock { get; } = new();
        public List<BufferPack> Buffers { get; } = new();

        public BufferManager(Program program)
        {
            Program = program;
        }

        public void AddBuffer(Vertex[] vertices, ushort[] indices)
        {
            lock (BufferLock)
            {
                CreateVertexBuffers(vertices, out var vertexBuffer, out var vertexBufferMemory);
                CreateIndexBuffer(indices, out var indexBuffer, out var indexBufferMemory);
                Buffers.Add(new(vertexBuffer, vertexBufferMemory, (uint)vertices.Length, indexBuffer, indexBufferMemory, (uint)indices.Length));
            }
        }

        public void CreateUniformBuffer()
        {
            var bufferSize = (uint)Unsafe.SizeOf<UniformBufferObject>();

            CreateBuffer(bufferSize, BufferUsageFlags.TransferSource, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent, out UniformStagingBuffer, out UniformStagingBufferMemory);
            CreateBuffer(bufferSize, BufferUsageFlags.TransferDestination | BufferUsageFlags.UniformBuffer, MemoryPropertyFlags.DeviceLocal, out UniformBuffer, out UniformBufferMemory);
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

        private void ReleaseUnmanagedResources()
        {
            Program = null;
            lock (BufferLock)
            {
                foreach (var (vertexBuffer, vertexBufferMemory, _, indexBuffer, indexBufferMemory, _) in Buffers)
                {
                    indexBuffer.Dispose();
                    vertexBuffer.Dispose();
                    indexBufferMemory.Free();
                    vertexBufferMemory.Free();
                }

                UniformBuffer.Dispose();
                UniformStagingBuffer.Dispose();
                UniformBufferMemory.Free();
                UniformStagingBufferMemory.Free();
            }
        }

        public void Dispose()
        {
            lock (BufferLock)
            {
                foreach (var mesh in Buffers)
                {
                    mesh.Dispose();
                }

                UniformBuffer.Dispose();
                UniformStagingBuffer.Dispose();
                UniformBufferMemory.Free();
                UniformStagingBufferMemory.Free();
            }
            GC.SuppressFinalize(this);
        }

        public void BindAllAndDraw(CommandBuffer commandBuffer)
        {
            lock (BufferLock)
            {
                foreach (var (vertexBuffer, _, _, indexBuffer, _, indicesLength) in Buffers)
                {
                    commandBuffer.BindVertexBuffers(0, vertexBuffer, 0);
                    commandBuffer.BindIndexBuffer(indexBuffer, 0, IndexType.Uint16);

                    commandBuffer.BindDescriptorSets(PipelineBindPoint.Graphics, Program.GraphicsManager.PipelineLayout, 0, Program.GraphicsManager.DescriptorSet, null);

                    commandBuffer.DrawIndexed(indicesLength, 1, 0, 0, 0);
                }
            }
        }
    }
}
