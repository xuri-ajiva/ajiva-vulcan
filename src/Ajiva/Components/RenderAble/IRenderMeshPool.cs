using SharpVk;

namespace Ajiva.Components.RenderAble;

public interface IRenderMeshPool
{
    uint LastMeshId { get; }
    void DrawMesh(CommandBuffer buffer, uint meshId);
    void Reset();
}