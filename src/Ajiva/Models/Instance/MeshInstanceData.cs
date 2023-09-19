using System.Numerics;
using System.Runtime.InteropServices;

namespace Ajiva.Models.Instance;

[StructLayout(LayoutKind.Sequential)]
public struct MeshInstanceData
{
    public Vector3 Position;
    public Vector3 Rotation;
    public Vector3 Scale;
    public uint TextureIndex;
    public Vector2 Padding;
}