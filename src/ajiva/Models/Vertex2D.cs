using System.Runtime.InteropServices;
using GlmSharp;
using SharpVk;
using SharpVk.Shanq;

namespace ajiva.Models
{
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

        public static VertexInputBindingDescription GetBindingDescription()
        {
            return new()
            {
                Binding = 0,
                Stride = (uint)Marshal.SizeOf<Vertex2D>(),
                InputRate = VertexInputRate.Vertex
            };
        }

        public static VertexInputAttributeDescription[] GetAttributeDescriptions()
        {
            return new VertexInputAttributeDescription[]
            {
                new()
                {
                    Binding = 0,
                    Location = 0,
                    Format = Format.R32G32SFloat,
                    Offset = (uint)Marshal.OffsetOf<Vertex2D>(nameof(Position))
                },
                new()
                {
                    Binding = 0,
                    Location = 1,
                    Format = Format.R32G32B32SFloat,
                    Offset = (uint)Marshal.OffsetOf<Vertex2D>(nameof(Colour))
                },
                new()
                {
                    Binding = 0,
                    Location = 2,
                    Format = Format.R32G32SFloat,
                    Offset = (uint)Marshal.OffsetOf<Vertex2D>(nameof(TextCoord))
                }
            };
        }
    }
}
