using ajiva.Models.Buffer;
using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace ajiva.Components.Mesh;

public class Mesh<T> : DisposingLogger, IMesh where T : struct
{
    public readonly ushort[] IndicesData;
    public readonly T[] VerticesData;
    private DeviceSystem? deviceComponent;
    private BufferOfT<ushort>? indexBuffer;
    private BufferOfT<T>? vertexBuffer;

    public Mesh(T[] verticesData, ushort[] indicesData)
    {
        VerticesData = verticesData;
        IndicesData = indicesData;

        MeshId = INextId<IMesh>.Next();
    }

    public IBufferOfT VertexBuffer => vertexBuffer;
    public IBufferOfT IndexBuffer => indexBuffer;

    /// <inheritdoc />
    public uint MeshId { get; set; }


    /// <inheritdoc />
    public void Create(DeviceSystem system)
    {
        if (deviceComponent != null) return; // if we have an deviceComponent we are created!
        deviceComponent = system;
        vertexBuffer = CreateShaderBuffer(VerticesData, BufferUsageFlags.VertexBuffer);
        indexBuffer = CreateShaderBuffer(IndicesData, BufferUsageFlags.IndexBuffer);
    }

    /// <inheritdoc />
    public void Bind(CommandBuffer commandBuffer)
    {
        ATrace.Assert(VertexBuffer != null, nameof(VertexBuffer) + " != null");
        ATrace.Assert(IndexBuffer != null, nameof(IndexBuffer) + " != null");
        commandBuffer.BindVertexBuffers(0, VertexBuffer.Buffer, 0);
        commandBuffer.BindIndexBuffer(IndexBuffer.Buffer, 0, IndexType.Uint16);
    }

    /// <inheritdoc />
    public void DrawIndexed(CommandBuffer commandBuffer)
    {
        ATrace.Assert(IndexBuffer != null, nameof(IndexBuffer) + " != null");
        commandBuffer.DrawIndexed((uint)indexBuffer.Length, 1, 0, 0, 0);
    }

    private BufferOfT<TV> CreateShaderBuffer<TV>(TV[] val, BufferUsageFlags bufferUsage) where TV : struct
    {
        ATrace.Assert(deviceComponent != null, nameof(deviceComponent) + " != null");

        BufferOfT<TV> aBuffer = new BufferOfT<TV>(val);
        var copyBuffer = CopyBuffer<TV>.CreateCopyBufferOnDevice(val, deviceComponent);

        aBuffer.Create(deviceComponent, BufferUsageFlags.TransferDestination | bufferUsage, MemoryPropertyFlags.DeviceLocal);

        copyBuffer.CopyTo(aBuffer, deviceComponent);
        copyBuffer.Dispose();
        return aBuffer;
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        vertexBuffer?.Dispose();
        indexBuffer?.Dispose();
        INextId<IMesh>.Remove(MeshId);
    }

    public Mesh<T> Clone()
    {
        return new Mesh<T>(VerticesData.ToArray(), IndicesData.ToArray());
    }
}
