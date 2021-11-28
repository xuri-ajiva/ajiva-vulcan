using ajiva.Components.Transform.Kd;

namespace ajiva.Components.Physics;

public interface IBoundingBox : IComponent
{
    KdVec MinPos { get; }
    KdVec MaxPos { get; }
    ICollider Collider { get; }
    KdTransform Center { get; }
    void ComputeBoxBackground();
}