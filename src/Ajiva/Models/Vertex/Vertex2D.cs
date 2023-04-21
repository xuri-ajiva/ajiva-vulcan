using System.Numerics;
using System.Runtime.InteropServices;
using SharpVk;
using SharpVk.Shanq;

namespace Ajiva.Models.Vertex;

public struct Vertex2D
{
    public Vertex2D(Vector2 position, Vector3 colour, Vector2 textCoord)
    {
        Position = position;
        Colour = colour;
        TextCoord = textCoord;
    }

    public Vertex2D(Vector2 position, Vector3 colour)
    {
        Position = position;
        Colour = colour;
        TextCoord = position;
    }

    [Location(0)] public Vector2 Position;
    [Location(1)] public Vector3 Colour;
    [Location(3)] public Vector2 TextCoord;

    public static VertexInputBindingDescription GetBindingDescription(uint binding)
    {
        return new VertexInputBindingDescription(binding, (uint)Marshal.SizeOf<Vertex2D>(), VertexInputRate.Vertex);
    }

    public static IEnumerable<VertexInputAttributeDescription> GetAttributeDescriptions(uint binding)
    {
        yield return new VertexInputAttributeDescription(0, binding, Format.R32G32SFloat, (uint)Marshal.OffsetOf<Vertex2D>(nameof(Position)));
        yield return new VertexInputAttributeDescription(1, binding, Format.R32G32B32SFloat, (uint)Marshal.OffsetOf<Vertex2D>(nameof(Colour)));
        yield return new VertexInputAttributeDescription(2, binding, Format.R32G32SFloat, (uint)Marshal.OffsetOf<Vertex2D>(nameof(TextCoord)));
    }
}
