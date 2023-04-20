using System.Drawing;
using System.Runtime.InteropServices.ComTypes;
using ajiva.Application;
using ajiva.Components.Media;
using ajiva.Systems.VulcanEngine.Interfaces;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Systems;

public class TextureSystem : ComponentSystemBase<TextureComponent>, ITextureSystem
{
    private readonly TextureCreator _creator;
    private readonly ShaderConfig _config;

    public TextureSystem(TextureCreator creator, Config globalConfig)
    {
        _creator = creator;
        _config = globalConfig.ShaderConfig;
        INextId<ATexture>.MaxId = (uint)_config.TEXTURE_SAMPLER_COUNT;
        TextureSamplerImageViews = new DescriptorImageInfo[_config.TEXTURE_SAMPLER_COUNT];
        Textures = new List<ATexture>();

        Default = _creator.FromFile("Logos:logo.png");
        Textures.Add(Default);
        for (var i = 0; i < _config.TEXTURE_SAMPLER_COUNT; i++) 
            TextureSamplerImageViews[i] = Default.DescriptorImageInfo;
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
        if (_config.TEXTURE_SAMPLER_COUNT <= texture.TextureId)
            throw new ArgumentException($"{nameof(texture.TextureId)} is more then {nameof(_config.TEXTURE_SAMPLER_COUNT)}", nameof(IBindCtx));

        TextureSamplerImageViews[texture.TextureId] = texture.DescriptorImageInfo;
    }

    public override TextureComponent CreateComponent(IEntity entity)
    {
        return new TextureComponent();
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        for (var i = 0; i < _config.TEXTURE_SAMPLER_COUNT; i++) TextureSamplerImageViews[i] = default;
        foreach (var texture in Textures) texture.Dispose();
    }

    public ATexture CreateTextureAndMapToDescriptor(Bitmap bitmap)
    {
        var texture = _creator.FromBitmap(bitmap);
        AddAndMapTextureToDescriptor(texture);
        return texture;
    }

    public Sampler CreateTextureSampler() => _creator.CreateTextureSampler();
}
