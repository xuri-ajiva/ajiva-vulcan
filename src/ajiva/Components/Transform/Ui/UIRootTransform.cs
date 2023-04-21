using System.Numerics;

namespace ajiva.Components.Transform.Ui;

public class UIRootTransform : IUiTransform
{
    private Rect2Di displaySize;

    public UIRootTransform(int displayWidth, int displayHeight , float min, float max)
    {
        renderSize = new Rect2Df(min, min, max, max);
        displaySize = new Rect2Di(0, 0, displayWidth, displayHeight);
    }

    /// <inheritdoc />
    public IUiTransform? Parent
    {
        get => null;
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public UiAnchor VerticalAnchor { get; set; } = default;

    /// <inheritdoc />
    public UiAnchor HorizontalAnchor { get; set; } = default;

    /// <inheritdoc />
    public Vector2 Rotation { get; set; } = Vector2.Zero;

    /// <inheritdoc />
    public Rect2Di DisplaySize
    {
        get => displaySize;
        set
        {
            displaySize = value;
            RecalculateSizes();
        }
    }

    /// <inheritdoc />
    public Rect2Df RenderSize
    {
        get => renderSize;
        set
        {
            renderSize = value;
            RecalculateSizes();
        }
    }

    /// <inheritdoc />
    public void RecalculateSizes()
    {
        foreach (var uiTransform in children)
        {
            uiTransform.RecalculateSizes();
        }
    }

    /// <inheritdoc />
    public void AddChild(IUiTransform child)
    {
        if (child is null)
            throw new ArgumentNullException(nameof(child));
        child.Parent = this;
        children.Add(child);
    }

    /// <inheritdoc />
    public void RemoveChild(IUiTransform child)
    {
        if (child is null)
            throw new ArgumentNullException(nameof(child));
        child.Parent = null;
        children.Remove(child);
    }

    private readonly List<IUiTransform> children = new List<IUiTransform>();
    private Rect2Df renderSize;
}
