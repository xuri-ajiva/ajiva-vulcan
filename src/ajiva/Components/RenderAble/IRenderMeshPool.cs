using SharpVk;

namespace ajiva.Components.RenderAble
{
    public interface IRenderMeshPool
    {
        uint LastMeshId { get; }
        void DrawMesh(CommandBuffer buffer, uint meshId);
        void Reset();
    }
}