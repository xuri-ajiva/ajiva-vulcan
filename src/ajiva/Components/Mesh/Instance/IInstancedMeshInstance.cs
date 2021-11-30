using ajiva.Models.Instance;

namespace ajiva.Components.Mesh.Instance;

public interface IInstancedMeshInstance : IDisposable, INextId<IInstancedMeshInstance>
{
    public IInstancedMesh InstancedMesh { get; }
    public uint InstanceId { get; }
    
    public void UpdateData(Action<ByRef<MeshInstanceData>> data);
}
