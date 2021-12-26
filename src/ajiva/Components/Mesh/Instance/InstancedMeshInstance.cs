using ajiva.Models.Instance;

namespace ajiva.Components.Mesh.Instance;

public class InstancedMeshInstance : IInstancedMeshInstance
{
    public InstancedMeshInstance(IInstancedMesh instancedMesh)
    {
        InstancedMesh = instancedMesh;
        InstanceId = instancedMesh.AddInstance(this);
    }

    /// <inheritdoc />
    public IInstancedMesh InstancedMesh { get; }

    /// <inheritdoc />
    public uint InstanceId { get; }

    /// <inheritdoc />
    public void UpdateData(ActionRef<MeshInstanceData> data) => InstancedMesh.UpdateData(InstanceId, data);

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        InstancedMesh.RemoveInstance(this);
    }
}
