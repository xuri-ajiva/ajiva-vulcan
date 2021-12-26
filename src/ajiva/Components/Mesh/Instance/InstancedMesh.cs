using ajiva.Models.Buffer.Dynamic;
using ajiva.Models.Instance;

namespace ajiva.Components.Mesh.Instance;

public class InstancedMesh : IInstancedMesh
{
    private DynamicUniversalDedicatedBufferArray<MeshInstanceData> instanceDataBuffer;

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
    public void UpdateData(uint instanceId, ActionRef<MeshInstanceData> action) => instanceDataBuffer.Update((int)instanceId, action);

    /// <inheritdoc />
    public uint AddInstance(IInstancedMeshInstance instancedMeshInstance)
    {
        return instanceDataBuffer.Add(new MeshInstanceData());
    }

    /// <inheritdoc />
    public void RemoveInstance(IInstancedMeshInstance instancedMeshInstance)
    {
        instanceDataBuffer.RemoveAt(instancedMeshInstance.InstanceId);
    }

    public void SetInstanceDataBuffer(DynamicUniversalDedicatedBufferArray<MeshInstanceData> instanceDataBuffer) => this.instanceDataBuffer = instanceDataBuffer;

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        INextId<IInstancedMesh>.Remove(InstancedId);
    }
}
