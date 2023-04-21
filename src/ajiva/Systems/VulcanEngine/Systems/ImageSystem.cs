using ajiva.Components.Media;
using ajiva.Systems.VulcanEngine.Interfaces;
using SharpVk;
using SharpVk.NVidia;
using Buffer = SharpVk.Buffer;

namespace ajiva.Systems.VulcanEngine.Systems;

public class ImageSystem : ComponentSystemBase<AImage>, IImageSystem
{
    private readonly IDeviceSystem _deviceSystem;

    /// <inheritdoc />
    public ImageSystem(IDeviceSystem deviceSystem)
    {
        _deviceSystem = deviceSystem;
    }

    /// <inheritdoc />
    public void Init()
    {
    }

    public AImage CreateImageAndView(uint width, uint height, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, ImageAspectFlags aspectFlags)
    {
        var aImage = new AImage(true);
        var device = _deviceSystem.Device!;

        aImage.Image = device.CreateImage(ImageType.Image2d, format, new Extent3D(width, height, 1), 1, 1, SampleCountFlags.SampleCount1, tiling, usage, SharingMode.Exclusive, ArrayProxy<uint>.Null, ImageLayout.Undefined);

        var memRequirements = device.GetImageMemoryRequirements2(new ImageMemoryRequirementsInfo2
        {
            Image = aImage.Image
        });

        aImage.Memory = device.AllocateMemory(memRequirements.MemoryRequirements.Size, _deviceSystem.FindMemoryType(memRequirements.MemoryRequirements.MemoryTypeBits, properties), new DedicatedAllocationMemoryAllocateInfo
        {
            Image = aImage.Image
        });

        device.BindImageMemory2(new BindImageMemoryInfo
        {
            Image = aImage.Image,
            Memory = aImage.Memory,
            MemoryOffset = 0
        });

        aImage.CreateView(device, format, aspectFlags);
        _deviceSystem.WatchObject(aImage);

        return aImage;
    }

    public AImage CreateManagedImage(Format format, ImageAspectFlags aspectFlags, Extent2D extent)
    {
        var aImage = CreateImageAndView(extent.Width, extent.Height, format, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachment, MemoryPropertyFlags.DeviceLocal, aspectFlags);

        TransitionImageLayout(aImage.Image!, format, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal);
        return aImage;
    }

    public AImage CreateManagedImage(Format format, ImageAspectFlags aspectFlags, Canvas canvas)
    {
        var aImage = CreateImageAndView(canvas.Width, canvas.Height, format, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachment, MemoryPropertyFlags.DeviceLocal, aspectFlags);

        TransitionImageLayout(aImage.Image!, format, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal);
        return aImage;
    }

#region imageHelp

    public void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height)
    {
        _deviceSystem.ExecuteSingleTimeCommand(QueueType.TransferQueue, CommandPoolSelector.Transit, command =>
        {
            command.CopyBufferToImage(buffer, image, ImageLayout.TransferDestinationOptimal, new BufferImageCopy
            {
                BufferOffset = 0,
                BufferRowLength = 0,
                BufferImageHeight = 0,
                ImageOffset = new Offset3D(),
                ImageExtent = new Extent3D(width, height, 1),
                ImageSubresource = new ImageSubresourceLayers
                {
                    AspectMask = ImageAspectFlags.Color,
                    MipLevel = 0,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                }
            });
        });
    }

    public void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout)
    {
        var subresourceRange = new ImageSubresourceRange
        {
            BaseMipLevel = 0,
            LevelCount = 1,
            BaseArrayLayer = 0,
            LayerCount = 1
        };

        if (newLayout == ImageLayout.DepthStencilAttachmentOptimal)
        {
            subresourceRange.AspectMask = ImageAspectFlags.Depth;

            if (format.HasStencilComponent()) subresourceRange.AspectMask |= ImageAspectFlags.Stencil;
        }
        else
        {
            subresourceRange.AspectMask = ImageAspectFlags.Color;
        }

        var barrier = new ImageMemoryBarrier
        {
            OldLayout = oldLayout,
            NewLayout = newLayout,
            Image = image,
            SubresourceRange = subresourceRange
        };

        PipelineStageFlags sourceStage;
        PipelineStageFlags destinationStage;

        switch (oldLayout)
        {
            case ImageLayout.Undefined when newLayout == ImageLayout.TransferDestinationOptimal:
                barrier.SourceAccessMask = 0;
                barrier.DestinationAccessMask = AccessFlags.TransferWrite;

                sourceStage = PipelineStageFlags.TopOfPipe;
                destinationStage = PipelineStageFlags.Transfer;
                break;
            case ImageLayout.TransferDestinationOptimal when newLayout == ImageLayout.ShaderReadOnlyOptimal:
                barrier.SourceAccessMask = AccessFlags.TransferWrite;
                barrier.DestinationAccessMask = AccessFlags.ShaderRead;

                sourceStage = PipelineStageFlags.Transfer;
                destinationStage = PipelineStageFlags.FragmentShader;
                break;
            case ImageLayout.Undefined when newLayout == ImageLayout.DepthStencilAttachmentOptimal:
                barrier.SourceAccessMask = 0;
                barrier.DestinationAccessMask = AccessFlags.DepthStencilAttachmentRead | AccessFlags.DepthStencilAttachmentWrite;

                sourceStage = PipelineStageFlags.TopOfPipe;
                destinationStage = PipelineStageFlags.EarlyFragmentTests;
                break;
            default:
                throw new ArgumentException("unsupported layout transition!");
        }

        _deviceSystem.ExecuteSingleTimeCommand(QueueType.GraphicsQueue, CommandPoolSelector.Foreground, command => command.PipelineBarrier(sourceStage, destinationStage, ArrayProxy<MemoryBarrier>.Null, ArrayProxy<BufferMemoryBarrier>.Null, barrier));
    }
    
#endregion
    public override AImage CreateComponent(IEntity entity)
    {
        return new AImage(false){}; //todo what to set data to?
    }
}
