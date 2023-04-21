using ajiva.Components.Mesh;
using ajiva.utils.Changing;

namespace ajiva.Components.Physics;

public interface ICollider : IComponent
{
    public IChangingObserver ChangingObserver { get; }

    uint MeshId { get; set; }

    MeshPool Pool { get; set; }
    bool IsStatic { get; set; }
    void ResolveCollision(ICollider itemCollider);
}
