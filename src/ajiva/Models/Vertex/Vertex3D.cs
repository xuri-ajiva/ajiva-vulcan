using System.Runtime.InteropServices;
using GlmSharp;
using SharpVk;
using SharpVk.Shanq;

namespace ajiva.Models.Vertex;

public struct Vertex3D
{
    public Vertex3D(vec3 position, vec3 colour, vec2 textCoord)
    {
        Position = position;
        Colour = colour;
        TextCoord = textCoord;
    }

    public Vertex3D(vec3 position, vec3 colour)
    {
        Position = position;
        Colour = colour;
        TextCoord = position.xy;
    }

    [Location(0)] public vec3 Position;
    [Location(1)] public vec3 Colour;
    [Location(3)] public vec2 TextCoord;

    public static VertexInputBindingDescription GetBindingDescription(uint binding)
    {
        return new VertexInputBindingDescription
        {
            Binding = binding,
            Stride = (uint)Marshal.SizeOf<Vertex3D>(),
            InputRate = VertexInputRate.Vertex
        };
    }

    public static IEnumerable<VertexInputAttributeDescription> GetAttributeDescriptions(uint binding)
    {
        yield return new VertexInputAttributeDescription(0, binding, Format.R32G32B32SFloat, (uint)Marshal.OffsetOf<Vertex3D>(nameof(Position)));
        yield return new VertexInputAttributeDescription(1, binding, Format.R32G32B32SFloat, (uint)Marshal.OffsetOf<Vertex3D>(nameof(Colour)));
        yield return new VertexInputAttributeDescription(2, binding, Format.R32G32SFloat, (uint)Marshal.OffsetOf<Vertex3D>(nameof(TextCoord)));
    }
}
