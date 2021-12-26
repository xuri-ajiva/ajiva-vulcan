using ajiva.Ecs;
using ajiva.Utils.Changing;
using SharpVk;

namespace ajiva.Components.Mesh.Instance;

public interface IInstanceMeshPool : IAjivaEcsObject, IDisposable
{
    public IChangingObserver<IInstanceMeshPool> Changed { get; }
    IInstancedMesh AsInstanced(IMesh mesh);
    void AddInstanced(IMesh mesh);

    IInstancedMeshInstance CreateInstance(IInstancedMesh instancedMesh);
    IInstancedMeshInstance CreateInstance(uint instancedMeshId);

    void DeleteInstance(IInstancedMeshInstance instance);

    void DrawInstanced(IInstancedMesh instancedMesh, CommandBuffer renderBuffer, uint vertexBufferBindId, uint instanceBufferBindId);
    void CommitInstanceDataChanges();
}
