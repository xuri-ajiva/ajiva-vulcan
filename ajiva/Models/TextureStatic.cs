using System.Drawing;
using System.Drawing.Imaging;
using ajiva.Engine;
using ajiva.EngineManagers;
using SharpVk;

namespace ajiva.Models
{
    public partial class Texture
    {
        public static Texture FromFile(IRenderEngine renderEngine, string path)
        {
            return new()
            {
                Image = CreateTextureImageFromFile(renderEngine, path),
                Sampler = CreateTextureSampler(renderEngine.DeviceComponent)
            };
        }

        private static AImage CreateTextureImageFromFile(IRenderEngine renderEngine, string fileName)
        {
            var img = System.Drawing.Image.FromFile(fileName);
            var bm = new Bitmap(img);

            var texWidth = (uint)bm.Width;
            var texHeight = (uint)bm.Height;
            var imageSize = texWidth * texHeight * 4u;

            using ABuffer aBuffer = new(imageSize);
            aBuffer.Create(renderEngine.DeviceComponent, BufferUsageFlags.TransferSource, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCached);

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

            AImage aImage = renderEngine.ImageComponent.CreateImageAndView(texWidth, texHeight, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDestination | ImageUsageFlags.Sampled, MemoryPropertyFlags.DeviceLocal, ImageAspectFlags.Color);

            renderEngine.ImageComponent.TransitionImageLayout(aImage.Image!, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDestinationOptimal);
            renderEngine.ImageComponent.CopyBufferToImage(aBuffer.Buffer!, aImage.Image, texWidth, texHeight);
            renderEngine.ImageComponent.TransitionImageLayout(aImage.Image, Format.R8G8B8A8Srgb, ImageLayout.TransferDestinationOptimal, ImageLayout.ShaderReadOnlyOptimal);

            return aImage;
        }

        private static Sampler CreateTextureSampler(DeviceComponent deviceComponent)
        {
            var properties = deviceComponent.PhysicalDevice!.GetProperties();

            var textureSampler = deviceComponent.Device!.CreateSampler(Filter.Linear, Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.Repeat,
                SamplerAddressMode.Repeat, SamplerAddressMode.Repeat, default, true, properties.Limits.MaxSamplerAnisotropy,
                false, CompareOp.Always, default, default, BorderColor.IntOpaqueBlack, false);
            return textureSampler;
        }
    }
}