using ajiva.Components.Transform.SpatialAcceleration;

namespace ajiva.Components.Physics;

public interface IBoundingBox : IComponent
{
    StaticOctalSpace Space { get; }
    ICollider Collider { get; }
    void ComputeBoxBackground();
    void SetTree(StaticOctalTreeContainer<BoundingBox> octalTree);
    void RemoveTree();
}
