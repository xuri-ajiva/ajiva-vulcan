using System.Runtime.InteropServices;
using GlmSharp;
using SharpVk;
using SharpVk.Shanq;

namespace ajiva.Models.Vertex;

public struct Vertex2D
{
    public Vertex2D(vec2 position, vec3 colour, vec2 textCoord)
    {
        Position = position;
        Colour = colour;
        TextCoord = textCoord;
    }

    public Vertex2D(vec2 position, vec3 colour)
    {
        Position = position;
        Colour = colour;
        TextCoord = position;
    }

    [Location(0)] public vec2 Position;
    [Location(1)] public vec3 Colour;
    [Location(3)] public vec2 TextCoord;

    public static VertexInputBindingDescription GetBindingDescription(uint binding)
    {
        return new VertexInputBindingDescription(binding, (uint)Marshal.SizeOf<Vertex2D>(), VertexInputRate.Vertex);
    }

    public static IEnumerable<VertexInputAttributeDescription> GetAttributeDescriptions(uint binding)
    {
        yield return new VertexInputAttributeDescription(location: 0, binding: binding, format: Format.R32G32SFloat, offset: (uint)Marshal.OffsetOf<Vertex2D>(nameof(Position)));
        yield return new VertexInputAttributeDescription(location: 1, binding: binding, format: Format.R32G32B32SFloat, offset: (uint)Marshal.OffsetOf<Vertex2D>(nameof(Colour)));
        yield return new VertexInputAttributeDescription(location: 2, binding: binding, format: Format.R32G32SFloat, offset: (uint)Marshal.OffsetOf<Vertex2D>(nameof(TextCoord)));
    }
}
