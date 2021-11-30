using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Instance;

namespace ajiva.Components.Mesh.Instance;

public class InstancedMesh : IInstancedMesh
{
    private AChangeAwareBackupBufferOfT<MeshInstanceData> instanceDataBuffer;

    public InstancedMesh(IMesh mesh)
    {
        Mesh = mesh;
        InstancedId = INextId<IInstancedMesh>.Next();
    }

    /// <inheritdoc />
    public IMesh Mesh { get; }

    /// <inheritdoc />
    public uint InstancedId { get; }

    /// <inheritdoc />
    public void UpdateData(uint instanceId, Action<ByRef<MeshInstanceData>> data) => data.Invoke(instanceDataBuffer.GetForChange((int)instanceId));

    public void SetInstanceDataBuffer(AChangeAwareBackupBufferOfT<MeshInstanceData> instanceDataBuffer) => this.instanceDataBuffer = instanceDataBuffer;

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        INextId<IInstancedMesh>.Remove(InstancedId);
    }
}
