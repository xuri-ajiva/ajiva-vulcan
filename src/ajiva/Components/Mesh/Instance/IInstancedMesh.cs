using ajiva.Models.Instance;

namespace ajiva.Components.Mesh.Instance;

public interface IInstancedMesh : IDisposable, INextId<IInstancedMesh>
{
    public IMesh Mesh { get; }
    public uint InstancedId { get; }
    public void UpdateData(uint instanceId, Action<ByRef<MeshInstanceData>> data);
}
