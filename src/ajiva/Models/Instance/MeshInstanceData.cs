using System.Runtime.InteropServices;
using GlmSharp;

namespace ajiva.Models.Instance;

[StructLayout(LayoutKind.Sequential)]
public struct MeshInstanceData
{
    public vec3 Position;
    public vec3 Rotation;
    public vec3 Scale;
    public uint TextureIndex;
    public vec2 Padding;
}
