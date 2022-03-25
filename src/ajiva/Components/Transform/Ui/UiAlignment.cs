namespace ajiva.Components.Transform.Ui;

[Flags]
public enum UiAlignment : byte
{
    None = 0,
    Min = 1 << 0,
    Max = 1 << 1,
    Center = Min | Max,

    Top = Vertical | Min,
    Bottom = Vertical | Max,
    Left = Horizontal | Min,
    Right = Horizontal | Max,

    CenterHorizontal = Center | Horizontal,
    CenterVertical = Center | Vertical,

    Horizontal = 1 << 6,
    Vertical = 1 << 7,

    AxisMask = 0b11000000,
    AlignmentMask = 0b00111111,
}
public enum UiAlignmentOrigin : byte
{
    None = 0,
    Min = 1 << 0,
    Max = 1 << 1,
    Center = Min | Max,
}
public enum UiAxis : byte
{
    None = 0,
    Horizontal = 1 << 6,
    Vertical = 1 << 7,
}
