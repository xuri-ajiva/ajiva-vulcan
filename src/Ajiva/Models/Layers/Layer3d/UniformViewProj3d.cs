using System.Numerics;

namespace Ajiva.Models.Layers.Layer3d;

public struct UniformViewProj3d : IComp<UniformViewProj3d>
{
    public Matrix4x4 View;
    public Matrix4x4 Proj;

    /// <inheritdoc />
    public bool CompareTo(UniformViewProj3d other)
    {
        return View == other.View && Proj == other.Proj;
    }
}
