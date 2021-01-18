using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using ajiva.Engine;
using ajiva.Models;
using SharpVk;

namespace ajiva.EngineManagers
{
    public class TextureComponent : RenderEngineComponent
    {
        public const int MAX_TEXTURE_SAMPLERS_IN_SHADER = 128;

        public TextureComponent(IRenderEngine renderEngine) : base(renderEngine)
        {
            TextureSamplerImageViews = new DescriptorImageInfo[MAX_TEXTURE_SAMPLERS_IN_SHADER];
            Textures = new();
        }

        public Texture? Default { get; private set; }
        private List<Texture> Textures { get; }
        public DescriptorImageInfo[] TextureSamplerImageViews { get; }

        private AImage CreateTextureImageFromFile(string fileName)
        {
            var img = System.Drawing.Image.FromFile(fileName);
            var bm = new Bitmap(img);

            var texWidth = (uint)bm.Width;
            var texHeight = (uint)bm.Height;
            var imageSize = texWidth * texHeight * 4u;

            using ABuffer aBuffer = new(imageSize);
            aBuffer.Create(RenderEngine.DeviceComponent, BufferUsageFlags.TransferSource, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCached);

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

            AImage aImage = RenderEngine.ImageComponent.CreateImageAndView(texWidth, texHeight, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDestination | ImageUsageFlags.Sampled, MemoryPropertyFlags.DeviceLocal, ImageAspectFlags.Color);

            RenderEngine.ImageComponent.TransitionImageLayout(aImage.Image, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDestinationOptimal);
            RenderEngine.ImageComponent.CopyBufferToImage(aBuffer.Buffer!, aImage.Image, texWidth, texHeight);
            RenderEngine.ImageComponent.TransitionImageLayout(aImage.Image, Format.R8G8B8A8Srgb, ImageLayout.TransferDestinationOptimal, ImageLayout.ShaderReadOnlyOptimal);

            return aImage;
        }

        private Sampler CreateTextureSampler()
        {
            var properties = RenderEngine.DeviceComponent.PhysicalDevice!.GetProperties();

            var textureSampler = RenderEngine.DeviceComponent.Device!.CreateSampler(Filter.Linear, Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.Repeat,
                SamplerAddressMode.Repeat, SamplerAddressMode.Repeat, default, true, properties.Limits.MaxSamplerAnisotropy,
                false, CompareOp.Always, default, default, BorderColor.IntOpaqueBlack, false);
            return textureSampler;
        }

        public void AddAndMapTextureToDescriptor(Texture texture)
        {
            MapTextureToDescriptor(texture);
            Textures.Add(texture);
        }

        public void MapTextureToDescriptor(Texture texture)
        {
            if (MAX_TEXTURE_SAMPLERS_IN_SHADER <= texture.TextureId) throw new ArgumentException($"{nameof(texture.TextureId)} is more then {nameof(MAX_TEXTURE_SAMPLERS_IN_SHADER)}", nameof(IBindCtx));

            TextureSamplerImageViews[texture.TextureId] = texture.DescriptorImageInfo;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            for (var i = 0; i < MAX_TEXTURE_SAMPLERS_IN_SHADER; i++)
            {
                TextureSamplerImageViews[i] = default;
            }
            foreach (var texture in Textures)
            {
                texture.Dispose();
            }
        }

        public void EnsureDefaultImagesExists()
        {
            if (Default != null) return;
            RenderEngine.DeviceComponent.EnsureDevicesExist();
            
            Default = new(0)
            {
                Image = CreateTextureImageFromFile("logo.png"),
                Sampler = CreateTextureSampler()
            };
            for (var i = 0; i < MAX_TEXTURE_SAMPLERS_IN_SHADER; i++)
            {
                TextureSamplerImageViews[i] = Default.DescriptorImageInfo;
            }

            /* todo move int hot load
             AddAndMapTextureToDescriptor(new(1)
            {
                Image = CreateTextureImageFromFile("logo2.png"),
                Sampler = CreateTextureSampler()
            });*/
        }
    }
}
