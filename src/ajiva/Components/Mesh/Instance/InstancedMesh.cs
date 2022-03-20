using ajiva.Models.Buffer.Dynamic;

namespace ajiva.Components.Mesh.Instance;

public class InstancedMesh<T> : IInstancedMesh<T> where T : unmanaged
{
    private DynamicUniversalDedicatedBufferArray<T> instanceDataBuffer;

    public InstancedMesh(IMesh mesh)
    {
        Mesh = mesh;
        InstancedId = INextId<IInstancedMesh<T>>.Next();
    }

    /// <inheritdoc />
    public IMesh Mesh { get; }

    /// <inheritdoc />
    public uint InstancedId { get; }

    /// <inheritdoc />
    public void UpdateData(uint instanceId, ActionRef<T> action) => instanceDataBuffer.Update((int)instanceId, action);

    /// <inheritdoc />
    public uint AddInstance(IInstancedMeshInstance<T> instancedMeshInstance)
    {
        return instanceDataBuffer.Add(new T());
    }

    /// <inheritdoc />
    public void RemoveInstance(IInstancedMeshInstance<T> instancedMeshInstance)
    {
        instanceDataBuffer.RemoveAt(instancedMeshInstance.InstanceId);
    }

    public void SetInstanceDataBuffer(DynamicUniversalDedicatedBufferArray<T> pInstanceDataBuffer) => instanceDataBuffer = pInstanceDataBuffer;

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        INextId<IInstancedMesh<T>>.Remove(InstancedId);
    }
}
