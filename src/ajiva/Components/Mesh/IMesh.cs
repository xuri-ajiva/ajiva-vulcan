using Ajiva.Models.Buffer;
using Ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace Ajiva.Components.Mesh;

public interface IMesh : IDisposingLogger
{
    uint MeshId { get; set; }
    IBufferOfT VertexBuffer { get; }
    IBufferOfT IndexBuffer { get; }
    void Create(DeviceSystem system);

    void Bind(CommandBuffer commandBuffer);

    void DrawIndexed(CommandBuffer commandBuffer);
}
