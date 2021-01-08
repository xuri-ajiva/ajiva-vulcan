using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ajiva.Engine;
using ajiva.Models;
using SharpVk;
using Buffer = SharpVk.Buffer;

namespace ajiva.EngineManagers
{
    public class BufferManager : IDisposable
    {
        private readonly IEngine engine;
        public Buffer UniformStagingBuffer;
        public Buffer UniformBuffer;
        public DeviceMemory UniformStagingBufferMemory;
        public DeviceMemory UniformBufferMemory;

        public object BufferLock { get; } = new();
        public List<Mesh> Buffers { get; } = new();

        public BufferManager(IEngine engine)
        {
            this.engine = engine;
        }

        public void AddBuffer(Vertex[] vertices, ushort[] indices)
        {
            lock (BufferLock)
            {
                Buffers.Add(new(engine.DeviceManager, vertices, indices));
            }
        }

        public void CreateUniformBuffer()
        {
            var bufferSize = (uint)Unsafe.SizeOf<UniformBufferObject>();

            engine.DeviceManager.CreateBuffer(bufferSize, BufferUsageFlags.TransferSource, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent, out UniformStagingBuffer, out UniformStagingBufferMemory);
            engine.DeviceManager.CreateBuffer(bufferSize, BufferUsageFlags.TransferDestination | BufferUsageFlags.UniformBuffer, MemoryPropertyFlags.DeviceLocal, out UniformBuffer, out UniformBufferMemory);
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
