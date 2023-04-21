using ajiva.Components.RenderAble;
using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace ajiva.Components.Mesh;

public class MeshPool : IMeshPool
{
    private readonly DeviceSystem deviceSystem;

    public MeshPool(DeviceSystem deviceSystem)
    {
        this.deviceSystem = deviceSystem;
    }

    public Dictionary<uint, IMesh> Meshes { get; } = new Dictionary<uint, IMesh>();

    public RenderInstanceReadyMeshPool Use()
    {
        return new RenderInstanceReadyMeshPool(this);
    }

    public IMesh GetMesh(uint meshId)
    {
        if(Meshes.TryGetValue(meshId, out var mesh)) return mesh;
        Log.Warning("Mesh not found, returning error mesh");
        AddMesh(MeshPrefab.Error);
        return Meshes[meshId];
    }

    public void AddMesh(IMesh mesh)
    {
        if(Meshes.ContainsKey(mesh.MeshId)) return; //mesh already added
        mesh.Create(deviceSystem);
        Meshes.Add(mesh.MeshId, mesh); //todo add check if already added
    }
}
public class RenderInstanceReadyMeshPool : IRenderMeshPool
{
    private static readonly object Lock = new object();
    private readonly MeshPool meshPool;

    public RenderInstanceReadyMeshPool(MeshPool meshPool)
    {
        this.meshPool = meshPool;
    }

    /// <inheritdoc />
    public uint LastMeshId { get; private set; } = uint.MaxValue;

    /// <inheritdoc />
    public void DrawMesh(CommandBuffer buffer, uint meshId)
    {
        var mesh = meshPool.Meshes[meshId]; // todo: check if exists and take error mesh

        if (meshId != LastMeshId)
        {
            LastMeshId = meshId;
            mesh.Bind(buffer);
        }
        lock (Lock)
        {
            mesh.DrawIndexed(buffer);
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        LastMeshId = uint.MaxValue;
    }
}
