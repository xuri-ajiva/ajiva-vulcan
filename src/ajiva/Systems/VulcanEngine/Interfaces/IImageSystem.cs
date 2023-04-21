using ajiva.Components.Media;
using SharpVk;
using Buffer = SharpVk.Buffer;

namespace ajiva.Systems.VulcanEngine.Interfaces;

public interface IImageSystem   : IComponentSystem<AImage>
{
    AImage CreateImageAndView(uint width, uint height, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, ImageAspectFlags aspectFlags);
    AImage CreateManagedImage(Format format, ImageAspectFlags aspectFlags, Extent2D extent);
    AImage CreateManagedImage(Format format, ImageAspectFlags aspectFlags, Canvas canvas);
    void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height);
    void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout);

}
