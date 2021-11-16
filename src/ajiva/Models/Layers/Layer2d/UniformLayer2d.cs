using System.Runtime.InteropServices;
using GlmSharp;

namespace ajiva.Models.Layers.Layer2d;

[StructLayout(LayoutKind.Sequential)]
public struct UniformLayer2d : IComp<UniformLayer2d>
{
    public mat4 View;
    public mat4 Proj;
    public vec2 MousePos;
    public vec2 Vec2;

    /// <inheritdoc />
    public bool CompareTo(UniformLayer2d other)
    {
        return false;
    }
}