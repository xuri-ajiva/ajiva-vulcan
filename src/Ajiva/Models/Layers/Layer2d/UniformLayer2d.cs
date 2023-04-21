using System.Numerics;
using System.Runtime.InteropServices;

namespace Ajiva.Models.Layers.Layer2d;

[StructLayout(LayoutKind.Sequential)]
public struct UniformLayer2d : IComp<UniformLayer2d>
{
    public Matrix4x4 View;
    public Matrix4x4 Proj;
    public Vector2 MousePos;
    public Vector2 Vec2;

    /// <inheritdoc />
    public bool CompareTo(UniformLayer2d other)
    {
        return false;
    }
}