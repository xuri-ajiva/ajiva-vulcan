using System;
using System.Drawing;
using System.Drawing.Imaging;
using ajiva.Engine;
using ajiva.Models;
using SharpVk;

namespace ajiva.EngineManagers
{
    public class TextureManager : IEngineManager
    {
        private readonly IEngine engine;

        public TextureManager(IEngine engine)
        {
            this.engine = engine;
            Logo = null!;
        }

        public Texture Logo { get; private set; }

        private AImage CreateTextureImageFromFile(string fileName)
        {
            AImage aImage;
            unsafe
            {
                var img = System.Drawing.Image.FromFile(fileName);
                var bm = new Bitmap(img);
                var scp0 = bm.LockBits(new(0, 0, bm.Width, bm.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);

                var texWidth = (uint)bm.Width;
                var texHeight = (uint)bm.Height;
                var imageSize = texWidth * texHeight * 4u;

                engine.DeviceManager.CreateBuffer(imageSize, BufferUsageFlags.TransferSource, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCached, out var stagingBuffer, out var stagingBufferMemory);

                var map = stagingBufferMemory.Map(0, imageSize, MemoryMapFlags.None);

                ImageHelper.ArgbCopyMap((ImageHelper.Argb32R*)scp0.Scan0.ToPointer(), (ImageHelper.Rgba32*)map.ToPointer(), texWidth * texHeight);
                //ImageHelper.ArgbCopyMap((byte*)scp0.Scan0.ToPointer(), (byte*)map.ToPointer(), texWidth * texHeight);
                //System.Buffer.MemoryCopy(scp0.Scan0.ToPointer(), map.ToPointer(), imageSize, imageSize);

                stagingBufferMemory.Unmap();
                bm.UnlockBits(scp0);

                stagingBufferMemory.Unmap();

                aImage = engine.ImageManager.CreateImageAndView(texWidth, texHeight, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDestination | ImageUsageFlags.Sampled, MemoryPropertyFlags.DeviceLocal, ImageAspectFlags.Color);

                engine.ImageManager.TransitionImageLayout(aImage.Image, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDestinationOptimal);
                engine.ImageManager.CopyBufferToImage(stagingBuffer, aImage.Image, texWidth, texHeight);
                engine.ImageManager.TransitionImageLayout(aImage.Image, Format.R8G8B8A8Srgb, ImageLayout.TransferDestinationOptimal, ImageLayout.ShaderReadOnlyOptimal);

                stagingBuffer.Destroy();
                stagingBufferMemory.Free();
            }

            return aImage;
        }

        private Sampler CreateTextureSampler()
        {
            PhysicalDeviceProperties properties = engine.DeviceManager.PhysicalDevice.GetProperties();

            var textureSampler = engine.DeviceManager.Device.CreateSampler(Filter.Linear, Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.Repeat,
                SamplerAddressMode.Repeat, SamplerAddressMode.Repeat, default, true, properties.Limits.MaxSamplerAnisotropy,
                false, CompareOp.Always, default, default, BorderColor.IntOpaqueBlack, false);
            return textureSampler;
        }

        public void CreateLogo()
        {
            Logo = new();
            Logo.Image = CreateTextureImageFromFile("logo.png");
            Logo.Sampler = CreateTextureSampler();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Logo.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
