using System;
using ajiva.EngineManagers;
using SharpVk;

namespace ajiva.Models
{
    public class Mesh : IDisposable
    {
        private readonly DeviceManager manager;
        public BufferOfT<Vertex> Vertices { get; }
        public BufferOfT<ushort> Indeces { get; }

        public Mesh(DeviceManager manager, Vertex[] vertices, ushort[] indices)
        {
            this.manager = manager;
            Vertices = CreateShaderBuffer(vertices, BufferUsageFlags.VertexBuffer);
            Indeces = CreateShaderBuffer(indices, BufferUsageFlags.IndexBuffer);
        }

        private BufferOfT<T> CreateShaderBuffer<T>(T[] val, BufferUsageFlags bufferUsage) where T : notnull
        {
            BufferOfT<T> aBuffer = new(val);
            var copyBuffer = CopyBuffer<T>.CreateCopyBufferOnDevice(val, manager);

            aBuffer.Create(manager.Device, BufferUsageFlags.TransferDestination | bufferUsage, type => manager.FindMemoryType(type, MemoryPropertyFlags.DeviceLocal));

            copyBuffer.CopyTo(aBuffer, manager);
            copyBuffer.Dispose();
            return aBuffer;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Vertices.Dispose();
            Indeces.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Bind(CommandBuffer commandBuffer)
        {
            commandBuffer.BindVertexBuffers(0, Vertices.Buffer, 0);
            commandBuffer.BindIndexBuffer(Indeces.Buffer, 0, IndexType.Uint16);
        }

        public void DrawIndexed(CommandBuffer commandBuffer)
        {
            commandBuffer.DrawIndexed((uint)Indeces.Length, 1, 0, 0, 0);
        }
    }
}
