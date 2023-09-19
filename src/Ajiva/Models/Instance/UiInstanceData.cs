using System.Numerics;
using System.Runtime.InteropServices;

namespace Ajiva.Models.Instance;

[StructLayout(LayoutKind.Sequential)]
public struct UiInstanceData
{
    public Vector2 Offset;
    public Vector2 Scale;
    public Vector2 Rotation;
    public uint TextureIndex;
    public UiDrawType DrawType;
}
public enum UiDrawType : uint
{
    TexturedRectangle,
    Button,
    Ellipse
    //TODO
}