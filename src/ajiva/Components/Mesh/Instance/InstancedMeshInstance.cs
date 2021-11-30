using ajiva.Models.Instance;

namespace ajiva.Components.Mesh.Instance;

public class InstancedMeshInstance : IInstancedMeshInstance
{
    public InstancedMeshInstance(IInstancedMesh instancedMesh)
    {
        InstancedMesh = instancedMesh;
        InstanceId = INextId<IInstancedMeshInstance>.Next();
    }

    /// <inheritdoc />
    public IInstancedMesh InstancedMesh { get; }

    /// <inheritdoc />
    public uint InstanceId { get; }

    /// <inheritdoc />
    public void UpdateData(Action<ByRef<MeshInstanceData>> data) => InstancedMesh.UpdateData(InstanceId, data);

    /// <inheritdoc />
    public ByRef<MeshInstanceData> Data { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        INextId<IInstancedMeshInstance>.Remove(InstanceId);
    }
}
