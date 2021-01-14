using System;
using System.Diagnostics;
using ajiva.Engine;
using ajiva.EngineManagers;
using SharpVk;

namespace ajiva.Models
{
    public partial class Mesh : DisposingLogger
    {
        public readonly Vertex[] VerticesData;
        public readonly ushort[] IndicesData;
        private DeviceComponent? deviceComponent;
        public BufferOfT<Vertex>? Vertices { get; protected set; }
        public BufferOfT<ushort>? Indeces { get; protected set; }

        public Mesh(Vertex[] verticesData, ushort[] indicesData)
        {
            this.VerticesData = verticesData;
            this.IndicesData = indicesData;
        }

        public void Create(DeviceComponent component)
        {
            this.deviceComponent = component;
            Vertices = CreateShaderBuffer(VerticesData, BufferUsageFlags.VertexBuffer);
            Indeces = CreateShaderBuffer(IndicesData, BufferUsageFlags.IndexBuffer);
        }

        private BufferOfT<T> CreateShaderBuffer<T>(T[] val, BufferUsageFlags bufferUsage) where T : notnull
        {
            ATrace.Assert(deviceComponent != null, nameof(deviceComponent) + " != null");
            
            BufferOfT<T> aBuffer = new(val);
            var copyBuffer = CopyBuffer<T>.CreateCopyBufferOnDevice(val, deviceComponent);

            aBuffer.Create(deviceComponent, BufferUsageFlags.TransferDestination | bufferUsage, MemoryPropertyFlags.DeviceLocal);

            copyBuffer.CopyTo(aBuffer, deviceComponent);
            copyBuffer.Dispose();
            return aBuffer;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            Vertices?.Dispose();
            Indeces?.Dispose();
        }

        public void Bind(CommandBuffer commandBuffer)
        {
            ATrace.Assert(Vertices != null, nameof(Vertices) + " != null");
            ATrace.Assert(Indeces != null, nameof(Indeces) + " != null");
            commandBuffer.BindVertexBuffers(0, Vertices.Buffer, 0);
            commandBuffer.BindIndexBuffer(Indeces.Buffer, 0, IndexType.Uint16);
        }

        public void DrawIndexed(CommandBuffer commandBuffer)
        {
            ATrace.Assert(Indeces != null, nameof(Indeces) + " != null");
            commandBuffer.DrawIndexed((uint)Indeces.Length, 1, 0, 0, 0);
        }
    }
}
