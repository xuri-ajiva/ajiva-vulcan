namespace ajiva.Components.Mesh.Instance;

public interface IInstancedMesh<T> : IDisposable where T : unmanaged
{
    public IMesh Mesh { get; }
    public uint InstancedId { get; }
    public void UpdateData(uint instanceId, ActionRef<T> action);
    uint AddInstance(IInstancedMeshInstance<T> instancedMeshInstance);
    void RemoveInstance(IInstancedMeshInstance<T> instancedMeshInstance);
}
