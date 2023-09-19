using System.Numerics;

namespace Ajiva.Components.Transform.Ui;

public readonly record struct Rect2Df(float MinX, float MinY, float MaxX, float MaxY)
{
    public float SizeX => MaxX - MinX;
    public float SizeY => MaxY - MinY;
    public float CenterX => MinX + SizeX / 2;
    public float CenterY => MinY + SizeY / 2;

    public Vector2 Min => new Vector2(MinX, MinY);
    public Vector2 Max => new Vector2(MaxX, MaxY);
}