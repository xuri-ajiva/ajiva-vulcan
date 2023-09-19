namespace Ajiva.Components.Mesh.Instance;

public class InstancedMeshInstance<T> : IInstancedMeshInstance<T> where T : unmanaged
{
    public InstancedMeshInstance(IInstancedMesh<T> instancedMesh)
    {
        InstancedMesh = instancedMesh;
        InstanceId = instancedMesh.AddInstance(this);
    }

    /// <inheritdoc />
    public IInstancedMesh<T> InstancedMesh { get; }

    /// <inheritdoc />
    public uint InstanceId { get; }

    /// <inheritdoc />
    public void UpdateData(ActionRef<T> data)
    {
        InstancedMesh.UpdateData(InstanceId, data);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        InstancedMesh.RemoveInstance(this);
    }
}