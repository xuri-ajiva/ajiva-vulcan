using ajiva.Components.RenderAble;
using ajiva.Utils.Changing;

namespace ajiva.Components.Physics;

public interface ICollider
{
    public IChangingObserver ChangingObserver { get; }

    uint MeshId { get; set; }

    MeshPool Pool { get; set; }
    bool IsStatic { get; set; }
}