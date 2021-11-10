using ajiva.Models;
using GlmSharp;

namespace ajiva.Components.RenderAble;

public class MeshPrefab
{
    //public static implicit operator ARenderAble(Mesh? mesh) => new(mesh, ARenderAble.NextId());

    public static Mesh<Vertex3D> Cube { get; } = new Mesh<Vertex3D>(new[]
    {
        new Vertex3D(new vec3(-1.0f, -1.0f, 1.0f), new vec3(1.0f, 0.0f, 0.0f)), new(new vec3(1.0f, -1.0f, 1.0f), new vec3(0.0f, 1.0f, 0.0f)), new(new vec3(1.0f, 1.0f, 1.0f), new vec3(0.0f, 0.0f, 1.0f)), new(new vec3(-1.0f, 1.0f, 1.0f), new vec3(0.0f, 0.0f, 0.0f)), new(new vec3(-1.0f, -1.0f, -1.0f), new vec3(1.0f, 0.0f, 0.0f)), new(new vec3(1.0f, -1.0f, -1.0f), new vec3(0.0f, 1.0f, 0.0f)), new(new vec3(1.0f, 1.0f, -1.0f), new vec3(0.0f, 0.0f, 1.0f)), new(new vec3(-1.0f, 1.0f, -1.0f), new vec3(0.0f, 0.0f, 0.0f))
    }, new ushort[]
    {
        0, 1, 2, 2, 3, 0, 1, 5, 6, 6, 2, 1, 7, 6, 5, 5, 4, 7, 4, 0, 3, 3, 7, 4, 4, 5, 1, 1, 0, 4, 3, 2, 6, 6, 7, 3
    });

    public static Mesh<Vertex2D> Rect { get; } = new Mesh<Vertex2D>(new[]
    {
        new Vertex2D(new vec2(0.0f, 0.0f), new vec3(0.0f, 0.0f, 0.0f)), new(new vec2(0.0f, 1.0f), new vec3(0.0f, 1.0f, 0.0f)), new(new vec2(1.0f, 0.0f), new vec3(1.0f, 0.0f, 1.0f)), new(new vec2(1.0f, 1.0f), new vec3(1.0f, 1.0f, 1.0f))
    }, new ushort[]
    {
        0, 1, 3, 2, 3, 0
    });

    public static Mesh<byte>? Empty { get; } = null;
}