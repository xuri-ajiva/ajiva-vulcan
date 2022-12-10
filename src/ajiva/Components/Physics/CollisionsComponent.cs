using ajiva.Components.Mesh;
using ajiva.Utils.Changing;

namespace ajiva.Components.Physics;

public class CollisionsComponent : ChangingComponentBase, ICollider
{
    private uint meshId;

    /// <inheritdoc />
    public CollisionsComponent() : base(0)
    {
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
            if (item.MeshId == MeshId)
            {
                return;
            }

            var mesh = Pool.GetMesh(MeshId);
            var itemMesh = Pool.GetMesh(item.MeshId);
            
        }
    }
}
