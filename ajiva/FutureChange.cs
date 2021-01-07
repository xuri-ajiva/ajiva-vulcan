using System;
using System.Threading.Tasks;
using ajiva.Models;
using GlmSharp;

namespace ajiva
{
    public partial class Program
    {
        private readonly ushort[] indices =
        {
            0, 1, 2, 2, 3, 0,
        };
        private readonly Vertex[] vertices = new[]
        {
            new Vertex(new vec2(-0.5f, -0.5f), new(1.0f, 0.0f, 0.0f), new(1.0f, 0.0f)), new Vertex(new vec2(0.5f, -0.5f), new(0.0f, 1.0f, 0.0f), new(0.0f, 0.0f)), new Vertex(new vec2(0.5f, 0.5f), new(0.0f, 0.0f, 1.0f), new(0.0f, 1.0f)), new Vertex(new vec2(-0.5f, 0.5f), new(1.0f, 1.0f, 1.0f), new(1.0f, 1.0f))
        };
    }
}
