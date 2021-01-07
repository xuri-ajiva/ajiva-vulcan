using System.Runtime.InteropServices;
using GlmSharp;
using SharpVk;
using SharpVk.Shanq;

namespace ajiva.Models
{
    public struct Vertex
    {
        public Vertex(vec3 position, vec3 colour, vec2 textCoord)
        {
            Position = position;
            Colour = colour;
            TextCoord = textCoord;
        }

        public Vertex(vec2 position, vec3 colour, vec2 textCoord)
        {
            Position = new(position, 0);
            Colour = colour;
            TextCoord = textCoord;
        }

        public Vertex(vec2 position, vec3 colour)
        {
            Position = new(position, 0);
            Colour = colour;
            TextCoord = position;
        }

        public Vertex(vec3 position, vec3 colour)
        {
            Position = position;
            Colour = colour;
            TextCoord = position.xy;
        }

        [Location(0)] public vec3 Position;
        [Location(1)] public vec3 Colour;
        [Location(3)] public vec2 TextCoord;

        public static VertexInputBindingDescription GetBindingDescription()
        {
            return new()
            {
                Binding = 0,
                Stride = (uint)Marshal.SizeOf<Vertex>(),
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
                    Format = Format.R32G32B32SFloat,
                    Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Position))
                },
                new()
                {
                    Binding = 0,
                    Location = 1,
                    Format = Format.R32G32B32SFloat,
                    Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Colour))
                },
                new()
                {
                    Binding = 0,
                    Location = 2,
                    Format = Format.R32G32SFloat,
                    Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(TextCoord))
                }
            };
        }
    }
}
