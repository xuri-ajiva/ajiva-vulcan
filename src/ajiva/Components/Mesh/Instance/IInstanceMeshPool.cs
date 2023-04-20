using ajiva.Utils.Changing;
using SharpVk;

namespace ajiva.Components.Mesh.Instance;

public interface IInstanceMeshPool<T> : IDisposable where T : unmanaged
{
    public IChangingObserver<IInstanceMeshPool<T>> Changed { get; }
    IInstancedMesh<T> AsInstanced(IMesh mesh);
    void AddInstanced(IMesh mesh);

    IInstancedMeshInstance<T> CreateInstance(IInstancedMesh<T> instancedMesh);
    IInstancedMeshInstance<T> CreateInstance(uint instancedMeshId);

    void DeleteInstance(IInstancedMeshInstance<T> instance);

    void DrawInstanced(IInstancedMesh<T> instancedMesh, CommandBuffer renderBuffer, uint vertexBufferBindId, uint instanceBufferBindId);
    void CommitInstanceDataChanges();
}
