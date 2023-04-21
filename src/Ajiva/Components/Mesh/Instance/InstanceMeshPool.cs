﻿using Ajiva.Models.Buffer.Dynamic;
using Ajiva.Systems.VulcanEngine.Interfaces;
using SharpVk;

namespace Ajiva.Components.Mesh.Instance;

public class InstanceMeshPool<T> : DisposingLogger, IInstanceMeshPool<T>, IUpdate where T : unmanaged
{
    private readonly object _lock = new object();
    private readonly IDeviceSystem deviceSystem;
    private readonly Dictionary<uint, uint> meshIdToInstancedMeshId = new Dictionary<uint, uint>();

    public InstanceMeshPool(IDeviceSystem deviceSystem)
    {
        this.deviceSystem = deviceSystem;
        Changed = new ChangingObserver<IInstanceMeshPool<T>>(this);
    }

    public Dictionary<uint, IInstancedMesh<T>> InstancedMeshes { get; } = new Dictionary<uint, IInstancedMesh<T>>();
    public Dictionary<uint, DynamicUniversalDedicatedBufferArray<T>> InstanceMeshData { get; } = new Dictionary<uint, DynamicUniversalDedicatedBufferArray<T>>();
    public IChangingObserver<IInstanceMeshPool<T>> Changed { get; }

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
        foreach (var instanceData in InstanceMeshData) instanceData.Value.CommitChanges();
    }

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        lock (_lock)
        {
            CommitInstanceDataChanges();
        }
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