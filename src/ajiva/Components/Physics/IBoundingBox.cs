using Ajiva.Components.Transform.SpatialAcceleration;

namespace Ajiva.Components.Physics;

public interface IBoundingBox : IComponent
{
    StaticOctalSpace Space { get; }
    void ComputeBoxBackground();
    void SetTree(StaticOctalTreeContainer<BoundingBox> octalTree);
    void RemoveTree();
}
