using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Ajiva.Models.Buffer.Dynamic;
using Ajiva.Systems.VulcanEngine.Interfaces;
using SharpVk;

namespace Ajiva.Components.Mesh.Instance;

public class InstanceMeshPool<T> : DisposingLogger, IInstanceMeshPool<T>, IUpdate where T : unmanaged
{
    private readonly IDeviceSystem deviceSystem;
    private readonly ConcurrentDictionary<uint, uint> meshIdToInstancedMeshId = new ConcurrentDictionary<uint, uint>();

    public InstanceMeshPool(IDeviceSystem deviceSystem)
    {
        this.deviceSystem = deviceSystem;
        Changed = new ChangingObserver<IInstanceMeshPool<T>>(this);
        _valueFactory = Factory;
    }

    public ConcurrentDictionary<uint, IInstancedMesh<T>> InstancedMeshes { get; } = new();
    public ConcurrentDictionary<uint, DynamicUniversalDedicatedBufferArray<T>> InstanceMeshData { get; } = new();
    public IChangingObserver<IInstanceMeshPool<T>> Changed { get; }

    /// <inheritdoc />
    public IInstancedMesh<T> AsInstanced(IMesh mesh)
    {
        /*if (!meshIdToInstancedMeshId.ContainsKey(mesh.MeshId))
            AddInstanced(mesh);
        ;*/
        return InstancedMeshes[meshIdToInstancedMeshId.GetOrAdd(mesh.MeshId, _valueFactory, mesh)];
    }

    private readonly Func<uint, IMesh, uint> _valueFactory;

    private uint Factory(uint arg1, IMesh mesh)
    {
        var instanceDataBuffer = new DynamicUniversalDedicatedBufferArray<T>(deviceSystem, 100, BufferUsageFlags.VertexBuffer);
        instanceDataBuffer.BufferResized.OnChanged += BufferResizedOnOnChanged;
        var iInstanceMesh = new InstancedMesh<T>(mesh);
        InstanceMeshData.TryAdd(iInstanceMesh.InstancedId, instanceDataBuffer);
        InstancedMeshes.TryAdd(iInstanceMesh.InstancedId, iInstanceMesh);
        iInstanceMesh.SetInstanceDataBuffer(instanceDataBuffer);
        return iInstanceMesh.InstancedId;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void AddInstanced(IMesh mesh)
    {
        var instanceDataBuffer = new DynamicUniversalDedicatedBufferArray<T>(deviceSystem, 100, BufferUsageFlags.VertexBuffer);
        instanceDataBuffer.BufferResized.OnChanged += BufferResizedOnOnChanged;
        var iInstanceMesh = new InstancedMesh<T>(mesh);
        InstanceMeshData.TryAdd(iInstanceMesh.InstancedId, instanceDataBuffer);
        InstancedMeshes.TryAdd(iInstanceMesh.InstancedId, iInstanceMesh);
        meshIdToInstancedMeshId.TryAdd(mesh.MeshId, iInstanceMesh.InstancedId);
        iInstanceMesh.SetInstanceDataBuffer(instanceDataBuffer);
    }

    /// <inheritdoc />
    public IInstancedMeshInstance<T> CreateInstance(IInstancedMesh<T> instancedMesh)
    {
        return new InstancedMeshInstance<T>(instancedMesh);
    }

    /// <inheritdoc />
    public IInstancedMeshInstance<T> CreateInstance(uint instancedMeshId)
    {
        return CreateInstance(InstancedMeshes[instancedMeshId]);
    }

    /// <inheritdoc />
    public void DeleteInstance(IInstancedMeshInstance<T> instance)
    {
        InstanceMeshData[instance.InstancedMesh.InstancedId].RemoveAt(instance.InstanceId);
        instance?.Dispose();
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
        foreach (var instanceData in InstanceMeshData) instanceData.Value.CommitChanges();
    }

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        CommitInstanceDataChanges();
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromMilliseconds(10));

    private void BufferResizedOnOnChanged(DynamicUniversalDedicatedBufferArray<T> sender)
    {
        Changed.Changed();
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        base.ReleaseUnmanagedResources(disposing);
        foreach (var (_, dynamicUniversalDedicatedBufferArray) in InstanceMeshData) dynamicUniversalDedicatedBufferArray.Dispose();
        InstancedMeshes.Clear();
        InstanceMeshData.Clear();
        meshIdToInstancedMeshId.Clear();
    }
}
