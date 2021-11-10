using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace ajiva.Components.RenderAble;

public class MeshPool
{
    private readonly DeviceSystem deviceSystem;

    public MeshPool(DeviceSystem deviceSystem)
    {
        this.deviceSystem = deviceSystem;
    }

    public Dictionary<uint, IMesh> Meshes { get; } = new();

    public RenderInstanceReadyMeshPool Use()
    {
        return new RenderInstanceReadyMeshPool(this);
    }

    public IMesh GetMesh(uint meshId)
    {
        return Meshes[meshId];
    }

    public void AddMesh(IMesh mesh)
    {
        mesh.Create(deviceSystem);
        Meshes.Add(mesh.MeshId, mesh);
    }
}
public class RenderInstanceReadyMeshPool : IRenderMeshPool
{
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
        IMesh mesh = meshPool.Meshes[meshId]; // todo: check if exists and take error mesh

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

    private static readonly object Lock = new();

    /// <inheritdoc />
    public void Reset()
    {
        LastMeshId = uint.MaxValue;
    }
}