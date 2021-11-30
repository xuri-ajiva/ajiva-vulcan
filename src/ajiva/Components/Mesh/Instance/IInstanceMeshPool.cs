using ajiva.Ecs;
using SharpVk;

namespace ajiva.Components.Mesh.Instance;

public interface IInstanceMeshPool : IAjivaEcsObject
{
    IInstancedMesh AsInstanced(IMesh mesh);
    void AddInstanced(IMesh mesh);

    IInstancedMeshInstance CreateInstance(IInstancedMesh instancedMesh);
    IInstancedMeshInstance CreateInstance(uint instancedMeshId);

    void DeleteInstance(IInstancedMeshInstance instance);

    void DrawInstanced(IInstancedMesh instancedMesh, CommandBuffer renderBuffer, uint vertexBufferBindId, uint instanceBufferBindId);
    void CommitInstanceDataChanges();
}
