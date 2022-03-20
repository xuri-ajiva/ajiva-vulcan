namespace ajiva.Components.Transform.Ui;

public record struct UiAnchor(UiAlignment Alignment, UiValueUnit Margin, UiValueUnit Span)
{
    public static UiAnchor Pixel(float margin, float span, UiAlignment alignment) => new UiAnchor(alignment, new UiValueUnit(margin, UiUnit.Pixel), new UiValueUnit(span, UiUnit.Pixel));
    public static UiAnchor Percent(float margin, float span, UiAlignment alignment) => new UiAnchor(alignment, new UiValueUnit(margin, UiUnit.Percent), new UiValueUnit(span, UiUnit.Percent));
    public static UiAnchor Zero => new UiAnchor(UiAlignment.None, UiValueUnit.Zero, UiValueUnit.Zero);
}