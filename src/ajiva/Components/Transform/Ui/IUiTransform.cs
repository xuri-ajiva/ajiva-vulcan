using GlmSharp;

namespace ajiva.Components.Transform.Ui;

public interface IUiTransform
{
    IUiTransform? Parent { get; set; }
    UiAnchor VerticalAnchor { get; set; }
    UiAnchor HorizontalAnchor { get; set; }
    vec2 Rotation { get; set; }
    Rect2Di DisplaySize { get; }
    Rect2Df RenderSize { get; }

    void RecalculateSizes();

    void AddChild(IUiTransform child);
    void RemoveChild(IUiTransform child);
}
