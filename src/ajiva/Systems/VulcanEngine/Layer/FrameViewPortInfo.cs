using ajiva.Components.Media;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Layer;

public record FrameViewPortInfo(Framebuffer Framebuffer, AImage FrameBufferImage, Extent2D Extent, Range DepthRange)
{
    public Viewport Viewport { get; } = new Viewport(0, 0, Extent.Width, Extent.Height, DepthRange.Start.Value, DepthRange.End.Value);
    public Rect2D FullRec { get; } = new Rect2D(Extent);
}
