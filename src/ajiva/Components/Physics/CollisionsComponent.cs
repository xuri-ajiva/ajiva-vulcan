using ajiva.Components.RenderAble;
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
}