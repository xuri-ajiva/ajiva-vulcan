using System.Runtime.InteropServices.ComTypes;
using ajiva.Application;
using ajiva.Components.Media;
using ajiva.Ecs;
using ajiva.Systems.Assets;
using ajiva.Systems.VulcanEngine.Interfaces;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Systems;

[Dependent(typeof(ImageSystem), typeof(AssetManager))]
public class TextureSystem : ComponentSystemBase<TextureComponent>, IInit, ITextureSystem
{
    private readonly ShaderConfig config;

    public TextureSystem(IAjivaEcs ecs) : base(ecs)
    {
        config = ecs.Get<Config>().ShaderConfig;
        INextId<ATexture>.MaxId = (uint)config.TEXTURE_SAMPLER_COUNT;
        TextureSamplerImageViews = new DescriptorImageInfo[config.TEXTURE_SAMPLER_COUNT];
        Textures = new List<ATexture>();
    }

    public ATexture? Default { get; private set; }
    private List<ATexture> Textures { get; }
    public DescriptorImageInfo[] TextureSamplerImageViews { get; }

    /// <inheritdoc />
    public void Init()
    {
        EnsureDefaultImagesExists(Ecs);
    }

    public void AddAndMapTextureToDescriptor(ATexture texture)
    {
        MapTextureToDescriptor(texture);
        Textures.Add(texture);
    }

    public void MapTextureToDescriptor(ATexture texture)
    {
        if (config.TEXTURE_SAMPLER_COUNT <= texture.TextureId) throw new ArgumentException($"{nameof(texture.TextureId)} is more then {nameof(config.TEXTURE_SAMPLER_COUNT)}", nameof(IBindCtx));

        TextureSamplerImageViews[texture.TextureId] = texture.DescriptorImageInfo;
    }

    /// <inheritdoc />
    public override TextureComponent CreateComponent(IEntity entity)
    {
        return new TextureComponent
        {
            TextureId = 0
        };
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        for (var i = 0; i < config.TEXTURE_SAMPLER_COUNT; i++) TextureSamplerImageViews[i] = default;
        foreach (var texture in Textures) texture.Dispose();
    }

    public void EnsureDefaultImagesExists(IAjivaEcs ecs)
    {
        if (Default != null) return;

        Default = ATexture.FromFile(ecs, "Logos:logo.png");
        Textures.Add(Default);

        for (var i = 0; i < config.TEXTURE_SAMPLER_COUNT; i++) TextureSamplerImageViews[i] = Default.DescriptorImageInfo;

        /* todo move int hot load
         AddAndMapTextureToDescriptor(new(1)
        {
            Image = CreateTextureImageFromFile("logo2.png"),
            Sampler = CreateTextureSampler()
        });*/
    }
}
