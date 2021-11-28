namespace ajiva.Components.Transform.Kd;

public interface IKdTransformReadOnly : IComponent
{
    IKdVecReadOnly Position { get; }
    IKdVecReadOnly Scale { get; }
    IKdVecReadOnly Rotation { get; }
}
public interface IKdTransform
{
    KdVec Position { get; }
    KdVec Scale { get; }
    KdVec Rotation { get; }
}
