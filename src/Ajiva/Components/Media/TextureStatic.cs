using System.Drawing;
using System.Drawing.Imaging;
using Ajiva.Assets;
using Ajiva.Assets.Contracts;
using Ajiva.Models.Buffer;
using Ajiva.Systems.VulcanEngine.Interfaces;
using SharpVk;
using Image = System.Drawing.Image;

namespace Ajiva.Components.Media;

public class TextureCreator
{
    private readonly IAssetManager _assetManager;
    private readonly IDeviceSystem _deviceSystem;
    private readonly IImageSystem _imageSystem;

    public TextureCreator(IDeviceSystem deviceSystem, IImageSystem imageSystem, IAssetManager assetManager)
    {
        _deviceSystem = deviceSystem;
        _imageSystem = imageSystem;
        _assetManager = assetManager;
    }

    public ATexture FromFile(string assetName)
    {
        return new ATexture {
            Image = CreateTextureImageFromAsset(assetName),
            Sampler = CreateTextureSampler()
        };
    }

    public ATexture FromBitmap(Bitmap bitmap)
    {
        return new ATexture {
            Image = CreateTextureImageFromBitmap(bitmap),
            Sampler = CreateTextureSampler()
        };
    }

    private AImage CreateTextureImageFromAsset(string assetName)
    {
        var img = Image.FromStream(_assetManager.GetAssetAsStream(AssetType.Texture, assetName));
        var bm = new Bitmap(img);
        return CreateTextureImageFromBitmap(bm);
    }

    private AImage CreateTextureImageFromBitmap(Bitmap bm)
    {
        var texWidth = (uint)bm.Width;
        var texHeight = (uint)bm.Height;
        var imageSize = texWidth * texHeight * 4u;

        using var aBuffer = new ABuffer(imageSize);
        aBuffer.Create(_deviceSystem, BufferUsageFlags.TransferSource, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCached);

        unsafe
        {
            var scp0 = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);

            using var map = aBuffer.MapDisposer();
            ImageHelper.ArgbCopyMap((ImageHelper.Argb32R*)scp0.Scan0.ToPointer(), (ImageHelper.Rgba32*)map.ToPointer(), texWidth * texHeight);
            //ImageHelper.ArgbCopyMap((byte*)scp0.Scan0.ToPointer(), (byte*)map.ToPointer(), texWidth * texHeight);
            //System.Buffer.MemoryCopy(scp0.Scan0.ToPointer(), map.ToPointer(), imageSize, imageSize);

            bm.UnlockBits(scp0);
        }

        var aImage = _imageSystem.CreateImageAndView(texWidth, texHeight, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDestination | ImageUsageFlags.Sampled, MemoryPropertyFlags.DeviceLocal, ImageAspectFlags.Color);

        _imageSystem.TransitionImageLayout(aImage.Image, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDestinationOptimal);
        _imageSystem.CopyBufferToImage(aBuffer.Buffer!, aImage.Image, texWidth, texHeight);
        _imageSystem.TransitionImageLayout(aImage.Image, Format.R8G8B8A8Srgb, ImageLayout.TransferDestinationOptimal, ImageLayout.ShaderReadOnlyOptimal);

        return aImage;
    }

    public Sampler CreateTextureSampler()
    {
        var properties = _deviceSystem.PhysicalDevice!.GetProperties();

        var textureSampler = _deviceSystem.Device!.CreateSampler(Filter.Linear, Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.Repeat,
            SamplerAddressMode.Repeat, SamplerAddressMode.Repeat, default, true, properties.Limits.MaxSamplerAnisotropy,
            false, CompareOp.Always, default, default, BorderColor.IntOpaqueBlack, false);
        return textureSampler;
    }
}
