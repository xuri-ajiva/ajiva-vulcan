namespace Ajiva.Components.Transform.Kd;

public interface IKdVecReadOnly
{
    public int Dimensions { get; }

    float this[int dimension] { get; }
}
public interface IKdVec
{
    public int Dimensions { get; }

    float this[int dimension] { get; set; }
}
