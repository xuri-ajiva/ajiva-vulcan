using System.Drawing;
using Ajiva.Components.Media;
using SharpVk;

namespace Ajiva.Systems.VulcanEngine.Interfaces;

public interface ITextureSystem : IComponentSystem<TextureComponent>
{
    ATexture? Default { get; }
    DescriptorImageInfo[] TextureSamplerImageViews { get; }
    void AddAndMapTextureToDescriptor(ATexture texture);
    void MapTextureToDescriptor(ATexture texture);

    ATexture CreateTextureAndMapToDescriptor(Bitmap bitmap);
    Sampler CreateTextureSampler();
}