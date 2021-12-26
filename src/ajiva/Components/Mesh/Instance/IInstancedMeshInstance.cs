using ajiva.Models.Instance;

namespace ajiva.Components.Mesh.Instance;

public interface IInstancedMeshInstance : IDisposable
{
    public IInstancedMesh InstancedMesh { get; }
    public uint InstanceId { get; }
    
    public void UpdateData(ActionRef<MeshInstanceData> action);
}
