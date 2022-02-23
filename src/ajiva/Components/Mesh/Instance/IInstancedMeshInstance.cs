namespace ajiva.Components.Mesh.Instance;

public interface IInstancedMeshInstance<T> : IDisposable where T : unmanaged
{
    public IInstancedMesh<T> InstancedMesh { get; }
    public uint InstanceId { get; }
    
    public void UpdateData(ActionRef<T> action);
}
