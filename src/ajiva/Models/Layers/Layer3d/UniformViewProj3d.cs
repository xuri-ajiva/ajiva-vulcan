using GlmSharp;

namespace ajiva.Models.Layers.Layer3d;

public struct UniformViewProj3d : IComp<UniformViewProj3d>
{
    public mat4 View;
    public mat4 Proj;

    /// <inheritdoc />
    public bool CompareTo(UniformViewProj3d other)
    {
        return View == other.View && Proj == other.Proj;
    }
}