using System.ComponentModel.DataAnnotations;
using Ajiva.Components;
using SharpVk;
using SharpVk.Khronos;

namespace Ajiva.Models;

public class SurfaceHandle : ChangingComponentBase
{
    private Surface? surface;

    /// <inheritdoc />
    public SurfaceHandle() : base(0)
    {
    }

    public Surface Surface
    {
        get => surface!;
        set => ChangingObserver.RaiseAndSetIfChanged(ref surface, value);
    }

    /// <param name="disposing"></param>
    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        try
        {
            surface?.Dispose();
        }
        catch (Exception e)
        {
            Log.Error(e, e.Message);
        }
    }

    public static implicit operator Surface(SurfaceHandle handle)
    {
        return handle.Surface;
    }
}
public class Canvas : DisposingLogger
{
    private Canvas? baseCanvas;
    private Rect2D rect;

    /// <inheritdoc />
    public Canvas(Rect2D rect, SurfaceHandle surface)
    {
        this.rect = rect;
        SurfaceHandle = surface;
    }

    /// <inheritdoc />
    public Canvas(SurfaceHandle surface) : this(new Rect2D(Offset2D.Zero, Extent2D.Zero), surface)
    {
    }

    private Rect2D BaseRect => baseCanvas?.BaseRect ?? rect;
    public SurfaceHandle SurfaceHandle { get; init; }

    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
    public bool HasSurface => SurfaceHandle.Surface is not null;
    public Rect2D Rect => rect;

    public Canvas Fork(Offset2D offsetBy, Extent2D shrinkBy)
    {
        var n = new Canvas(rect, SurfaceHandle) {
            Offset = offsetBy,
            baseCanvas = this
        };
        n.rect.Extent.Height -= shrinkBy.Height;
        n.rect.Extent.Width -= shrinkBy.Width;
        n.Validate();
        return n;
    }

    public Canvas Fork(Rect2D newRect)
    {
        ValidateThrow(newRect, rect); //todo user baseRect?, because it is the max valid ??
        return new Canvas(rect, SurfaceHandle) {
            rect = newRect,
            baseCanvas = this
        };
    }

    public void Validate()
    {
        ValidateThrow(rect, BaseRect);
    }

    private static void ValidateThrow(Rect2D current, Rect2D max)
    {
        if (!Validate(current, max))
            throw new ValidationException("The rect is Not inside the bounds of the root canvas");
    }

    private static bool Validate(Rect2D current, Rect2D max)
    {
        return !(current.Bottom > max.Bottom) && !(current.Right > max.Right) && !(current.Left < max.Left) && !(current.Top < max.Top);
    }

    /// <param name="disposing"></param>
    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        if (disposing)
            SurfaceHandle.Dispose();
    }

#region Extent

    public Extent2D Extent
    {
        get => rect.Extent;
        set => rect.Extent = value;
    }
    public uint Height
    {
        get => rect.Extent.Height;
        set => rect.Extent.Height = value;
    }
    public uint Width
    {
        get => rect.Extent.Width;
        set => rect.Extent.Width = value;
    }
    public float HeightF => rect.Extent.Height;
    public float WidthF => rect.Extent.Width;

    public int HeightI => (int)rect.Extent.Height;
    public int WidthI => (int)rect.Extent.Width;

#endregion

#region Offset

    public Offset2D Offset
    {
        get => rect.Offset;
        set => rect.Offset = value;
    }
    public int X
    {
        get => rect.Offset.X;
        set => rect.Offset.Y = value;
    }
    public int Y
    {
        get => rect.Offset.Y;
        set => rect.Offset.Y = value;
    }
    public float Xf => rect.Offset.X;
    public float Yf => rect.Offset.Y;

    public uint Xu => (uint)rect.Offset.X;
    public uint Yu => (uint)rect.Offset.Y;

#endregion
}