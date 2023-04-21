using System.Drawing;
using System.Runtime.InteropServices.ComTypes;
using Ajiva.Components.Media;
using Ajiva.Systems.VulcanEngine.Interfaces;
using SharpVk;

namespace Ajiva.Systems.VulcanEngine.Systems;

public class TextureSystem : ComponentSystemBase<TextureComponent>, ITextureSystem
{
    private readonly ShaderConfig _config;
    private readonly TextureCreator _creator;

    public TextureSystem(TextureCreator creator, AjivaConfig globalConfig)
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

    private List<ATexture> Textures { get; }

    public ATexture? Default { get; }
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

    public ATexture CreateTextureAndMapToDescriptor(Bitmap bitmap)
    {
        var texture = _creator.FromBitmap(bitmap);
        AddAndMapTextureToDescriptor(texture);
        return texture;
    }

    public Sampler CreateTextureSampler()
    {
        return _creator.CreateTextureSampler();
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        for (var i = 0; i < _config.TEXTURE_SAMPLER_COUNT; i++) TextureSamplerImageViews[i] = default;
        foreach (var texture in Textures) texture.Dispose();
    }
}