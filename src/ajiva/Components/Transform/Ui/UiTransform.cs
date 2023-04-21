using System.Diagnostics;
using System.Numerics;
using ajiva.utils.Changing;

namespace ajiva.Components.Transform.Ui;

public class UiTransform : DisposingLogger, IComponent, IUiTransform
{
    private UiAnchor verticalAnchor;
    private UiAnchor horizontalAnchor;
    private Vector2 rotation;
    private bool isDirty;
    private Rect2Di displaySize;
    private Rect2Df renderSize;
    private IUiTransform? parent;

    public UiTransform(IUiTransform? parent, UiAnchor verticalAnchor, UiAnchor horizontalAnchor, Vector2? rotation = null)
    {
        ChangingObserver = new ChangingObserver(0);

        Parent = parent;
        VerticalAnchor = verticalAnchor;
        HorizontalAnchor = horizontalAnchor;
        Rotation = rotation ?? new Vector2(0, 0);
    }

    private static (float pos, float span) ComputePos(UiAnchor uiAnchor, Rect2Di display, Rect2Df render)
    {
        var displayAxis = (UiAxis)(uiAnchor.Alignment & UiAlignment.AxisMask);
        var displayOrigin = (UiAlignmentOrigin)(uiAnchor.Alignment & UiAlignment.AlignmentMask);

        switch (displayAxis)
        {
            case UiAxis.Horizontal:
                var computeFixedValueSpanX = ComputeFixedValue(uiAnchor.Span, display.SizeX, render.SizeX);
                var computeFixedValueMarginX = ComputeFixedValue(uiAnchor.Margin, display.SizeX, render.SizeX);
                return displayOrigin switch
                {
                    UiAlignmentOrigin.Center => (render.CenterX - computeFixedValueSpanX / 2, computeFixedValueSpanX),
                    UiAlignmentOrigin.Min => (render.MinX + computeFixedValueMarginX, computeFixedValueSpanX),
                    UiAlignmentOrigin.Max => (render.MaxX - (computeFixedValueMarginX + computeFixedValueSpanX), computeFixedValueSpanX),
                    UiAlignmentOrigin.None => (0, 0),
                    _ => throw new ArgumentOutOfRangeException(nameof(uiAnchor.Alignment), displayOrigin, "Not a valid Origin")
                };
            case UiAxis.Vertical:
                var computeFixedValueSpanY = ComputeFixedValue(uiAnchor.Span, display.SizeY, render.SizeY);
                var computeFixedValueMarginY = ComputeFixedValue(uiAnchor.Margin, display.SizeY, render.SizeY);
                return displayOrigin switch
                {
                    UiAlignmentOrigin.Center => (render.CenterY - computeFixedValueSpanY / 2, computeFixedValueSpanY),
                    UiAlignmentOrigin.Min => (render.MinY + computeFixedValueMarginY, computeFixedValueSpanY),
                    UiAlignmentOrigin.Max => (render.MaxY - (computeFixedValueMarginY + computeFixedValueSpanY), computeFixedValueSpanY),
                    UiAlignmentOrigin.None => (0, 0),
                    _ => throw new ArgumentOutOfRangeException(nameof(uiAnchor.Alignment), displayOrigin, "Not a valid Origin")
                };

            case UiAxis.None: return (0, 0);
            default: throw new ArgumentOutOfRangeException(nameof(uiAnchor.Alignment), displayAxis, "Not a valid Axis");
        }
    }

    private static float ComputeFixedValue(UiValueUnit uiValue, int displaySpan, float renderSpan)
    {
        var (value, uiUnit) = uiValue;
        return uiUnit switch
        {
            UiUnit.Pixel => (value / displaySpan) * renderSpan,
            UiUnit.Percent => (value / 100f) * renderSpan,
            _ => throw new ArgumentOutOfRangeException(nameof(uiValue), "The " + nameof(UiUnit) + " value is out of Range")
        };
    }

#region Props

    public IChangingObserver ChangingObserver { get; }

    /// <inheritdoc />
    public IUiTransform? Parent
    {
        get => parent;
        set
        {
            if (parent == value) return;
            parent = value;
            isDirty = true;
            ChangingObserver.Changed();
        }
    }

    public UiAnchor VerticalAnchor
    {
        get => verticalAnchor;
        set
        {
            if (verticalAnchor == value) return;

            if ((value.Alignment & UiAlignment.AlignmentMask) == UiAlignment.None)
            {
                Log.Warning("Alignment Type Not set for Vertical Anchor Fixing...");
                value.Alignment |= UiAlignment.Vertical;
            }
            verticalAnchor = value;
            isDirty = true;
            ChangingObserver.Changed();
        }
    }
    public UiAnchor HorizontalAnchor
    {
        get => horizontalAnchor;
        set
        {
            if (horizontalAnchor == value) return;

            if ((value.Alignment & UiAlignment.AlignmentMask) == UiAlignment.None)
            {
                Log.Warning("Alignment Type Not set for Horizontal Anchor Fixing...");
                value.Alignment |= UiAlignment.Horizontal;
            }
            horizontalAnchor = value;
            isDirty = true;
            ChangingObserver.Changed();
        }
    }
    public Vector2 Rotation
    {
        get
        {
            if (Parent is not null)
                return rotation + Parent.Rotation;
            return rotation;
        }
        set => ChangingObserver.RaiseAndSetIfChanged(ref rotation, value);
    }

    /// <inheritdoc />
    public Rect2Di DisplaySize
    {
        get
        {
            if (isDirty) RecalculateSizes();
            return displaySize;
        }
    }

    /// <inheritdoc />
    public Rect2Df RenderSize
    {
        get
        {
            if (isDirty) RecalculateSizes();
            return renderSize;
        }
    }

#endregion

    public void RecalculateSizes()
    {
        isDirty = false;
        CalculateDisplaySize();
        renderSize = CalculateRenderSize();
        displaySize = CalculateDisplaySize();
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

    private Rect2Df CalculateRenderSize()
    {
        if (Parent is null) return new Rect2Df(0, 0, 0, 0);

        var (posX, spanX) = ComputePos(horizontalAnchor, Parent.DisplaySize, Parent.RenderSize);
        var (posY, spanY) = ComputePos(verticalAnchor, Parent.DisplaySize, Parent.RenderSize);

        return new Rect2Df(posX, posY, posX + spanX, posY + spanY);
    }

    private Rect2Di CalculateDisplaySize()
    {
        if (Parent is null) return new Rect2Di(0, 0, 0, 0);

        //unlerp the render size (value - min) / (max - min)

        var minX = (int)(Parent.DisplaySize.SizeX * ((RenderSize.MinX - Parent.RenderSize.MinX) / Parent.RenderSize.SizeX));
        var minY = (int)(Parent.DisplaySize.SizeY * ((RenderSize.MinY - Parent.RenderSize.MinY) / Parent.RenderSize.SizeY));

        var maxX = (int)(Parent.DisplaySize.SizeX * ((RenderSize.MaxX - Parent.RenderSize.MinX) / Parent.RenderSize.SizeX));
        var maxY = (int)(Parent.DisplaySize.SizeY * ((RenderSize.MaxY - Parent.RenderSize.MinY) / Parent.RenderSize.SizeY));

        var r= new Rect2Di(minX, minY, maxX - minX, maxY - minY);
        Log.Debug((GetHashCode().ToString("X8") +": "+ r));
        return r;
    }

    [Obsolete("Use RenderSize instead")]
    public RenderOffsetScale CalculateRenderOffsetScale()
    {
        if (Parent is null) return new RenderOffsetScale(new Vector2(0, 0), new Vector2(1, 1));

        var (posX, spanX) = ComputePos(horizontalAnchor, Parent.DisplaySize, Parent.RenderSize);
        var (posY, spanY) = ComputePos(verticalAnchor, Parent.DisplaySize, Parent.RenderSize);

        var ret = new RenderOffsetScale(new Vector2(posX, posY), new Vector2(spanX, spanY));
        Validate(ret, RenderSize);
        return ret;
    }

#region Validate

    [Conditional("DEBUG")]
    private static void Validate(RenderOffsetScale posVec, Rect2Df renderSize)
    {
        var (offset, scale) = posVec;
        Validate(offset, renderSize);
        Validate(offset + scale, renderSize);
    }

    private static void Validate(Vector2 posVec, Rect2Df renderSize)
    {
        Validate(posVec.X, renderSize.MinX, renderSize.MaxX);
        Validate(posVec.Y, renderSize.MinY, renderSize.MaxY);
    }

    private static void Validate(float pos, float min, float max)
    {
        if (pos < min)
            Log.Debug("Ui Component might be out of Visible Range");
        //throw new ArgumentOutOfRangeException(nameof(posVecY), posVecY, "The " + nameof(posVecY) + " value is out of Range");
        if (pos > max)
            Log.Debug("Ui Component might be out of Visible Range");
        //throw new ArgumentOutOfRangeException(nameof(posVecY), posVecY, "The " + nameof(posVecY) + " value is out of Range");
    }

#endregion
}
