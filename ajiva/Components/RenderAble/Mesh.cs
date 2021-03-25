using System.Linq;
using ajiva.Models.Buffer;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;
using SharpVk;

namespace ajiva.Components.RenderAble
{
    public partial class Mesh<T> : DisposingLogger where T : struct
    {
        public readonly T[] VerticesData;
        public readonly ushort[] IndicesData;
        private DeviceSystem? deviceComponent;
        public BufferOfT<T>? Vertices { get; protected set; }
        public BufferOfT<ushort>? Indeces { get; protected set; }

        public Mesh(T[] verticesData, ushort[] indicesData)
        {
            this.VerticesData = verticesData;
            this.IndicesData = indicesData;
        }

        public void Create(DeviceSystem system)
        {
            if (deviceComponent != null) return; // if we have an deviceComponent we are created!
            this.deviceComponent = system;
            Vertices = CreateShaderBuffer(VerticesData, BufferUsageFlags.VertexBuffer);
            Indeces = CreateShaderBuffer(IndicesData, BufferUsageFlags.IndexBuffer);
        }

        private BufferOfT<TV> CreateShaderBuffer<TV>(TV[] val, BufferUsageFlags bufferUsage) where TV : struct
        {
            ATrace.Assert(deviceComponent != null, nameof(deviceComponent) + " != null");
            
            BufferOfT<TV> aBuffer = new(val);
            var copyBuffer = CopyBuffer<TV>.CreateCopyBufferOnDevice(val, deviceComponent);

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

        public Mesh<T> Clone()
        {
            return new(VerticesData.ToArray(), IndicesData.ToArray());
        }
    }
}
