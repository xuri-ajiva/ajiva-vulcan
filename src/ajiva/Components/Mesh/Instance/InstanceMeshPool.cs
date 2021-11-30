using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Instance;
using ajiva.Systems.VulcanEngine.Interfaces;
using SharpVk;

namespace ajiva.Components.Mesh.Instance;

public class InstanceMeshPool : IInstanceMeshPool, IUpdate
{
    private readonly Dictionary<uint, IInstancedMesh> instanceMeshes = new();
    private readonly Dictionary<uint, AChangeAwareBackupBufferOfT<MeshInstanceData>> instanceMeshData = new();
    private readonly Dictionary<uint, uint> meshIdToInstancedMeshId = new();
    private readonly IDeviceSystem deviceSystem;

    public InstanceMeshPool(IDeviceSystem deviceSystem)
    {
        this.deviceSystem = deviceSystem;
    }

    /// <inheritdoc />
    public IInstancedMesh AsInstanced(IMesh mesh)
    {
        lock (_lock)
        {
            if (!meshIdToInstancedMeshId.ContainsKey(mesh.MeshId)) AddInstanced(mesh);
            return instanceMeshes[mesh.MeshId];
        }
    }

    /// <inheritdoc />
    public void AddInstanced(IMesh mesh)
    {
        lock (_lock)
        {
            var instanceDataBuffer = new AChangeAwareBackupBufferOfT<MeshInstanceData>(Const.Default.ModelBufferSize, deviceSystem, BufferUsageFlags.VertexBuffer);
            var iInstanceMesh = new InstancedMesh(mesh);
            instanceMeshData.Add(iInstanceMesh.InstancedId, instanceDataBuffer);
            instanceMeshes.Add(iInstanceMesh.InstancedId, iInstanceMesh);
            meshIdToInstancedMeshId.Add(mesh.MeshId, iInstanceMesh.InstancedId);
            iInstanceMesh.SetInstanceDataBuffer(instanceDataBuffer);
        }
    }

    /// <inheritdoc />
    public IInstancedMeshInstance CreateInstance(IInstancedMesh instancedMesh) => new InstancedMeshInstance(instancedMesh);

    /// <inheritdoc />
    public IInstancedMeshInstance CreateInstance(uint instancedMeshId) => new InstancedMeshInstance(instanceMeshes[instancedMeshId]);

    /// <inheritdoc />
    public void DeleteInstance(IInstancedMeshInstance instance)
    {
        lock (_lock)
        {
            instanceMeshData[instance.InstancedMesh.InstancedId][(int)instance.InstanceId] = new MeshInstanceData();
            instance?.Dispose();
        }
    }

    /// <inheritdoc />
    public void DrawInstanced(IInstancedMesh instancedMesh, CommandBuffer renderBuffer, uint vertexBufferBindId, uint instanceBufferBindId)
    {
        renderBuffer.BindVertexBuffers(vertexBufferBindId, instancedMesh.Mesh.VertexBuffer.Buffer, 0);
        renderBuffer.BindVertexBuffers(instanceBufferBindId, instanceMeshData[instancedMesh.InstancedId].Uniform.Buffer, 0);
        renderBuffer.BindIndexBuffer(instancedMesh.Mesh.IndexBuffer.Buffer, 0, IndexType.Uint16);
        renderBuffer.DrawIndexed((uint)instancedMesh.Mesh.IndexBuffer.Length, (uint)instanceMeshData[instancedMesh.InstancedId].Length, 0, 0, 0);
    }

    /// <inheritdoc />
    public void CommitInstanceDataChanges()
    {
        foreach (var instanceData in instanceMeshData)
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
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromMilliseconds(10));
}
