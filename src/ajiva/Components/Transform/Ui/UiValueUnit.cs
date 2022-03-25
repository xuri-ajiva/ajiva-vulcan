namespace ajiva.Components.Transform.Ui;

public record struct UiValueUnit(float Value, UiUnit Unit)
{
    public static UiValueUnit Zero { get; } = new UiValueUnit(0, 0);
    public static UiValueUnit Pixel(float value) => new UiValueUnit(value, UiUnit.Pixel);
    public static UiValueUnit Percent(float value) => new UiValueUnit(value, UiUnit.Percent);

    public static implicit operator UiValueUnit(float value) => Percent(value * 100);
    public static implicit operator UiValueUnit(uint value) => Pixel(value);
}
