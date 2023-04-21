using Ajiva.Components.Mesh;

namespace Ajiva.Components.Physics;

public class CollisionsComponent : ChangingComponentBase, ICollider
{
    private uint meshId;

    /// <inheritdoc />
    public CollisionsComponent(MeshPool pool) : base(0)
    {
        Pool = pool;
    }

    /// <inheritdoc />
    public uint MeshId
    {
        get => meshId;
        set => ChangingObserver.RaiseAndSetIfChanged(ref meshId, value);
    }

    /// <inheritdoc />
    public MeshPool Pool { get; set; }

    /// <inheritdoc />
    public bool IsStatic { get; set; }

    /// <inheritdoc />
    public void ResolveCollision(ICollider itemCollider)
    {
        if (itemCollider is CollisionsComponent item)
        {
            if (item.MeshId == MeshId) return;

            var mesh = Pool.GetMesh(MeshId);
            var itemMesh = Pool.GetMesh(item.MeshId);
        }
    }
}