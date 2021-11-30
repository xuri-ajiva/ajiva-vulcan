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
/*
 *     public vec4 Model1;
    public vec4 Model2;
    public vec4 Model3;
    public vec4 TextureSamplerId;
 */


