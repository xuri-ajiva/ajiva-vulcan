namespace ajiva.Components.Transform.Kd;

public class KdTransform : DisposingLogger, IKdTransform
{
    private readonly int dimensions;

    public KdTransform(int dimensions)
    {
        this.dimensions = dimensions;
        Scale = new KdVec(dimensions);
        Rotation = new KdVec(dimensions);
        Position = new KdVec(dimensions);
    }

    /// <inheritdoc />
    public KdVec Scale { get; }

    /// <inheritdoc />
    public KdVec Rotation { get; }

    /// <inheritdoc />
    public KdVec Position { get; }
}