using GlmSharp;

namespace ajiva.Components.Transform.Ui;

public readonly record struct Rect2Df(float MinX, float MinY, float MaxX, float MaxY)
{
    public float SizeX => MaxX - MinX;
    public float SizeY => MaxY - MinY;
    public float CenterX => MinX + SizeX / 2;
    public float CenterY => MinY + SizeY / 2;
    
    public vec2 Min => new vec2(MinX, MinY);
    public vec2 Max => new vec2(MaxX, MaxY);
}
