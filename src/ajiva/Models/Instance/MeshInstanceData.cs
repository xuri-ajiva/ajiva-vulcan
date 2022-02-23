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
[StructLayout(LayoutKind.Sequential)]
public struct Mesh2dInstanceData
{
    public vec2 Position;
    public vec2 Rotation;
    public vec2 Scale;
    public uint TextureIndex;
    public vec2 Padding;
}
/*
 *     public vec4 Model1;
    public vec4 Model2;
    public vec4 Model3;
    public vec4 TextureSamplerId;
 */
