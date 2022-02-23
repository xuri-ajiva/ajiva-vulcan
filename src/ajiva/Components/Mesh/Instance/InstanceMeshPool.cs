using ajiva.Models.Buffer.Dynamic;
using ajiva.Systems.VulcanEngine.Interfaces;
using ajiva.Utils.Changing;
using SharpVk;

namespace ajiva.Components.Mesh.Instance;

public class InstanceMeshPool<T> : DisposingLogger, IInstanceMeshPool<T>, IUpdate where T : unmanaged
{
    public Dictionary<uint, IInstancedMesh<T>> InstancedMeshes { get; } = new();
    public Dictionary<uint, DynamicUniversalDedicatedBufferArray<T>> InstanceMeshData { get; } = new();
    private readonly Dictionary<uint, uint> meshIdToInstancedMeshId = new();
    private readonly IDeviceSystem deviceSystem;
    public IChangingObserver<IInstanceMeshPool<T>> Changed { get; }

    public InstanceMeshPool(IDeviceSystem deviceSystem)
    {
        this.deviceSystem = deviceSystem;
        Changed = new ChangingObserver<IInstanceMeshPool<T>>(this);
    }

    /// <inheritdoc />
    public IInstancedMesh<T> AsInstanced(IMesh mesh)
    {
        lock (_lock)
        {
            if (!meshIdToInstancedMeshId.ContainsKey(mesh.MeshId)) AddInstanced(mesh);
            return InstancedMeshes[meshIdToInstancedMeshId[mesh.MeshId]];
        }
    }

    /// <inheritdoc />
    public void AddInstanced(IMesh mesh)
    {
        lock (_lock)
        {
            var instanceDataBuffer = new DynamicUniversalDedicatedBufferArray<T>(deviceSystem, 100, BufferUsageFlags.VertexBuffer);
            instanceDataBuffer.BufferResized.OnChanged += BufferResizedOnOnChanged;
            var iInstanceMesh = new InstancedMesh<T>(mesh);
            InstanceMeshData.Add(iInstanceMesh.InstancedId, instanceDataBuffer);
            InstancedMeshes.Add(iInstanceMesh.InstancedId, iInstanceMesh);
            meshIdToInstancedMeshId.Add(mesh.MeshId, iInstanceMesh.InstancedId);
            iInstanceMesh.SetInstanceDataBuffer(instanceDataBuffer);
        }
    }

    private void BufferResizedOnOnChanged(DynamicUniversalDedicatedBufferArray<T> sender)
    {
        Changed.Changed();
    }

    /// <inheritdoc />
    public IInstancedMeshInstance<T> CreateInstance(IInstancedMesh<T> instancedMesh)
    {
        return new InstancedMeshInstance<T>(instancedMesh);
    }

    /// <inheritdoc />
    public IInstancedMeshInstance<T> CreateInstance(uint instancedMeshId) => CreateInstance(InstancedMeshes[instancedMeshId]);

    /// <inheritdoc />
    public void DeleteInstance(IInstancedMeshInstance<T> instance)
    {
        lock (_lock)
        {
            InstanceMeshData[instance.InstancedMesh.InstancedId].RemoveAt(instance.InstanceId);
            instance?.Dispose();
        }
    }

    /// <inheritdoc />
    public void DrawInstanced(IInstancedMesh<T> instancedMesh, CommandBuffer renderBuffer, uint vertexBufferBindId, uint instanceBufferBindId)
    {
        renderBuffer.BindVertexBuffers(vertexBufferBindId, instancedMesh.Mesh.VertexBuffer.Buffer, 0);
        renderBuffer.BindVertexBuffers(instanceBufferBindId, InstanceMeshData[instancedMesh.InstancedId].Uniform.Current().Buffer, 0);
        renderBuffer.BindIndexBuffer(instancedMesh.Mesh.IndexBuffer.Buffer, 0, IndexType.Uint16);
        renderBuffer.DrawIndexed((uint)instancedMesh.Mesh.IndexBuffer.Length, (uint)InstanceMeshData[instancedMesh.InstancedId].Length, 0, 0, 0);
    }

    /// <inheritdoc />
    public void CommitInstanceDataChanges()
    {
        foreach (var instanceData in InstanceMeshData)
        {
            instanceData.Value.CommitChanges();
        }
    }

    private readonly object _lock = new();

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        lock (_lock)
        {
            CommitInstanceDataChanges();
        }
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        base.ReleaseUnmanagedResources(disposing);
        foreach (var (_, dynamicUniversalDedicatedBufferArray) in InstanceMeshData)
        {
            dynamicUniversalDedicatedBufferArray.Dispose();
        }
        InstancedMeshes.Clear();
        InstanceMeshData.Clear();
        meshIdToInstancedMeshId.Clear();
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromMilliseconds(10));
}
