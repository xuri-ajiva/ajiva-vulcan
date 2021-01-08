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
                foreach (var mesh in Buffers)
                {
                    mesh.Bind(commandBuffer);

                    commandBuffer.BindDescriptorSets(PipelineBindPoint.Graphics, engine.GraphicsManager.PipelineLayout, 0, engine.GraphicsManager.DescriptorSet, null);

                    mesh.DrawIndexed(commandBuffer);
                }
            }
        }
    }
}
