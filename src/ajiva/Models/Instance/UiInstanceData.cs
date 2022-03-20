using System.Runtime.InteropServices;
using GlmSharp;

namespace ajiva.Models.Instance;

[StructLayout(LayoutKind.Sequential)]
public struct UiInstanceData
{
    public vec2 Offset;
    public vec2 Scale;
    public vec2 Rotation;
    public uint TextureIndex;
    public UiDrawType DrawType;
}
public enum UiDrawType : uint
{
    TexturedRectangle,
    Button,
    Ellipse,
    //TODO
}
