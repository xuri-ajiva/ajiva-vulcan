namespace Ajiva.Components.Transform.Ui;

public readonly record struct Rect2Di(int X, int Y, int Width, int Height)
{
    public int SizeX => Width - X;
    public int SizeY => Height - Y;
    public int CenterX => X + SizeX / 2;
    public int CenterY => Y + SizeY / 2;
}
