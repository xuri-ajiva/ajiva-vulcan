using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using ajiva.Components;
using ajiva.Helpers;
using ajiva.Systems.VulcanEngine.Engine;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.EngineManagers
{
    public class TextureComponent : RenderEngineComponent
    {
        // ReSharper disable once InconsistentNaming
        public const int MAX_TEXTURE_SAMPLERS_IN_SHADER = 128;

        public TextureComponent(IRenderEngine renderEngine) : base(renderEngine)
        {
            INextId<ATexture>.MaxId = MAX_TEXTURE_SAMPLERS_IN_SHADER;
            TextureSamplerImageViews = new DescriptorImageInfo[MAX_TEXTURE_SAMPLERS_IN_SHADER];
            Textures = new();
        }

        public ATexture? Default { get; private set; }
        private List<ATexture> Textures { get; }
        public DescriptorImageInfo[] TextureSamplerImageViews { get; }
        
        public void AddAndMapTextureToDescriptor(ATexture texture)
        {
            MapTextureToDescriptor(texture);
            Textures.Add(texture);
        }

        public void MapTextureToDescriptor(ATexture texture)
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

            Default = ATexture.FromFile(RenderEngine,"logo.png");
            Textures.Add(Default);

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
