using System.Numerics;

namespace Ajiva.Models.Layers.Layer2d;

public struct SolidUniformModel2d : IComp<SolidUniformModel2d>
{
    public Matrix4x4 Model;

    /// <inheritdoc />
    public bool CompareTo(SolidUniformModel2d other)
    {
        return Model == other.Model;
    }
}
