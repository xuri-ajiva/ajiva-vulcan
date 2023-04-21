using ajiva.Systems.VulcanEngine;
using ajiva.utils.Changing;
using SharpVk;

namespace ajiva.Components.Media;

public class AImage : ChangingComponentBase
{
    private readonly bool disposeImage;
    private Image image = null!;
    private DeviceMemory memory = null!;
    private ImageView view = null!;

    public AImage(bool disposeImage) : base(20)
    {
        this.disposeImage = disposeImage;
    }

    public AImage() : this(false)
    {
    }

    public ImageView View
    {
        get => view;
        init => ChangingObserver.RaiseAndSetIfChanged(ref view, value);
    }
    public Image Image
    {
        get => image;
        set => ChangingObserver.RaiseAndSetIfChanged(ref image, value);
    }
    public DeviceMemory Memory
    {
        get => memory;
        set => ChangingObserver.RaiseAndSetIfChanged(ref memory, value);
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        if (disposeImage)
            Image?.Dispose();
        View?.Dispose();
        Memory?.Free();
    }

    public void CreateView(Device device, Format format, ImageAspectFlags aspectFlags)
    {
        view = image?.CreateImageView(device, format, aspectFlags);
    }
}