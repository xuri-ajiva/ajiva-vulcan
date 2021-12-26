using ajiva.Models.Instance;

namespace ajiva.Components.Mesh.Instance;

public interface IInstancedMesh : IDisposable
{
    public IMesh Mesh { get; }
    public uint InstancedId { get; }
    public void UpdateData(uint instanceId, ActionRef<MeshInstanceData> action);
    uint AddInstance(IInstancedMeshInstance instancedMeshInstance);
    void RemoveInstance(IInstancedMeshInstance instancedMeshInstance);
}
