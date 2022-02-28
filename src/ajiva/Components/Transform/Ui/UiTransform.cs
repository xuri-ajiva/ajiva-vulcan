using ajiva.Utils.Changing;
using GlmSharp;
using SharpVk;

namespace ajiva.Components.Transform.Ui;

public class UiTransform : DisposingLogger, IComponent
{
    private const float RenderMin = -1;
    private const float RenderMax = 1;
    private const float RenderCenter = 0;
    private const float RenderSize = 2;

    private UiAnchor verticalAnchor;
    private UiAnchor horizontalAnchor;
    private vec2 rotation;

    public UiTransform(UiAnchor verticalAnchor, UiAnchor horizontalAnchor, vec2? rotation = null)
    {
        ChangingObserver = new ChangingObserver(0);

        VerticalAnchor = verticalAnchor;
        HorizontalAnchor = horizontalAnchor;
        Rotation = rotation ?? new vec2(0, 0);
    }

    public vec4 GetRenderPoints(Extent2D extent)
    {
        var (posX, spanX) = ComputePos(horizontalAnchor, extent);
        var (posY, spanY) = ComputePos(verticalAnchor, extent);

        var ret = new vec4(posX, posY, posX + spanX, posY + spanY);
        Validate(ret);
        return ret;
    }

    private static void Validate(vec4 posVec)
    {
        Validate(posVec.x);
        Validate(posVec.y);
        Validate(posVec.z);
        Validate(posVec.w);
    }

    private static void Validate(float value)
    {
        if (value is > RenderMax or < RenderMin)
        {
            ALog.Debug("Ui Component might be out of Visible Range");
        }
    }

    private static (float pos, float span) ComputePos(UiAnchor uiAnchor, Extent2D extent2D)
    {
        float totalSpan = (uiAnchor.Alignment & UiAlignment.AxisMask) switch
        {
            UiAlignment.Horizontal => extent2D.Width,
            UiAlignment.Vertical => extent2D.Height,
            _ => throw new ArgumentOutOfRangeException(nameof(uiAnchor.Alignment), uiAnchor.Alignment & UiAlignment.AxisMask, "Not a valid Axis")
        };

        var computeFixedValueSpan = ComputeFixedValue(uiAnchor.Span, totalSpan);
        var computeFixedValueMargin = ComputeFixedValue(uiAnchor.Margin, totalSpan);

        if (uiAnchor.Alignment == UiAlignment.None) return (0, 0);

        if ((uiAnchor.Alignment & UiAlignment.Center) == UiAlignment.Center)
            return (RenderCenter - computeFixedValueSpan / 2, computeFixedValueSpan);
        
        if ((uiAnchor.Alignment & UiAlignment.Min) == UiAlignment.Min)
            return (RenderMin + computeFixedValueMargin, computeFixedValueSpan);
        if ((uiAnchor.Alignment & UiAlignment.Max) == UiAlignment.Max)
            return (RenderMax - (computeFixedValueMargin + computeFixedValueSpan), computeFixedValueSpan);

        throw new ArgumentOutOfRangeException(nameof(uiAnchor.Alignment), uiAnchor.Alignment, "");
    }

    private static float ComputeFixedValue(UiValueUnit uiValue, float totalSpan)
    {
        var (value, uiUnit) = uiValue;
        return uiUnit switch
        {
            UiUnit.Pixel => (value / totalSpan) * RenderSize,
            UiUnit.Percent => (value / 100f) * RenderSize,
            _ => throw new ArgumentOutOfRangeException(nameof(uiValue), "The " + nameof(UiUnit) + " value is out of Range")
        };
    }

    public IChangingObserver ChangingObserver { get; }
    public UiAnchor VerticalAnchor
    {
        get => verticalAnchor;
        set
        {
            if ((value.Alignment & UiAlignment.AlignmentMask) == UiAlignment.None)
            {
                ALog.Warn("Alignment Type Not set for Vertical Anchor Fixing...");
                value.Alignment |= UiAlignment.Vertical;
            }
            ChangingObserver.RaiseAndSetIfChanged(ref verticalAnchor, value);
        }
    }
    public UiAnchor HorizontalAnchor
    {
        get => horizontalAnchor;
        set
        {
            if ((value.Alignment & UiAlignment.AlignmentMask) == UiAlignment.None)
            {
                ALog.Warn("Alignment Type Not set for Horizontal Anchor Fixing...");
                value.Alignment |= UiAlignment.Horizontal;
            }
            ChangingObserver.RaiseAndSetIfChanged(ref horizontalAnchor, value);
        }
    }
    public vec2 Rotation
    {
        get => rotation;
        set => ChangingObserver.RaiseAndSetIfChanged(ref rotation, value);
    }
}
