using System.Drawing;
using System.Drawing.Imaging;
using ajiva.Ecs;
using ajiva.Models;
using ajiva.Models.Buffer;
using ajiva.Systems.Assets;
using ajiva.Systems.Assets.Contracts;
using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace ajiva.Components.Media;

public partial class ATexture
{
    public static ATexture FromFile(IAjivaEcs ecs, string assetName)
    {
        return new()
        {
            Image = CreateTextureImageFromAsset(ecs, assetName),
            Sampler = CreateTextureSampler(ecs.GetSystem<DeviceSystem>())
        };
    }

    public static ATexture FromBitmap(IAjivaEcs ecs, Bitmap bitmap)
    {
        return new()
        {
            Image = CreateTextureImageFromBitmap(ecs, bitmap),
            Sampler = CreateTextureSampler(ecs.GetSystem<DeviceSystem>())
        };
    }

    private static AImage CreateTextureImageFromAsset(IAjivaEcs ecs, string assetName)
    {
        var img = System.Drawing.Image.FromStream(ecs.GetSystem<AssetManager>().GetAssetAsStream(AssetType.Texture, assetName));
        var bm = new Bitmap(img);
        return CreateTextureImageFromBitmap(ecs, bm);
    }

    private static AImage CreateTextureImageFromBitmap(IAjivaEcs ecs, Bitmap bm)
    {
        var texWidth = (uint)bm.Width;
        var texHeight = (uint)bm.Height;
        var imageSize = texWidth * texHeight * 4u;

        using ABuffer aBuffer = new(imageSize);
        aBuffer.Create(ecs.GetSystem<DeviceSystem>(), BufferUsageFlags.TransferSource, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCached);

        unsafe
        {
            var scp0 = bm.LockBits(new(0, 0, bm.Width, bm.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);

            using (var map = aBuffer.MapDisposer())
            {
                ImageHelper.ArgbCopyMap((ImageHelper.Argb32R*)scp0.Scan0.ToPointer(), (ImageHelper.Rgba32*)map.ToPointer(), texWidth * texHeight);
                //ImageHelper.ArgbCopyMap((byte*)scp0.Scan0.ToPointer(), (byte*)map.ToPointer(), texWidth * texHeight);
                //System.Buffer.MemoryCopy(scp0.Scan0.ToPointer(), map.ToPointer(), imageSize, imageSize);

                bm.UnlockBits(scp0);
            }
        }

        var image = ecs.GetComponentSystem<ImageSystem, AImage>();

        AImage aImage = image.CreateImageAndView(texWidth, texHeight, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDestination | ImageUsageFlags.Sampled, MemoryPropertyFlags.DeviceLocal, ImageAspectFlags.Color);

        image.TransitionImageLayout(aImage.Image, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDestinationOptimal);
        image.CopyBufferToImage(aBuffer.Buffer!, aImage.Image, texWidth, texHeight);
        image.TransitionImageLayout(aImage.Image, Format.R8G8B8A8Srgb, ImageLayout.TransferDestinationOptimal, ImageLayout.ShaderReadOnlyOptimal);

        return aImage;
    }

    private static Sampler CreateTextureSampler(DeviceSystem deviceSystem)
    {
        var properties = deviceSystem.PhysicalDevice!.GetProperties();

        var textureSampler = deviceSystem.Device!.CreateSampler(Filter.Linear, Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.Repeat,
            SamplerAddressMode.Repeat, SamplerAddressMode.Repeat, default, true, properties.Limits.MaxSamplerAnisotropy,
            false, CompareOp.Always, default, default, BorderColor.IntOpaqueBlack, false);
        return textureSampler;
    }
}