using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using ajiva.Models;
using ajiva.Systems.VulcanEngine.Engine;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.EngineManagers
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

            Default = Texture.FromFile(RenderEngine,"logo.png");

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
