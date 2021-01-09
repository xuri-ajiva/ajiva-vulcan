using GlmSharp;
using SharpVk.Shanq;

namespace ajiva
{
    internal struct FragmentInput
    {
        [Location(0)] public vec3 Colour;
        [Location(1)] public vec4 Position;
    }
    public struct FragmentOutput
    {
        [Location(0)] public vec4 Colour;
    }
}
