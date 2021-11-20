using ajiva.Components.Media;
using ajiva.Ecs;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Interfaces;

public interface ITextureSystem : IComponentSystem<TextureComponent>
{
    ATexture? Default { get; }
    DescriptorImageInfo[] TextureSamplerImageViews { get; }
    void AddAndMapTextureToDescriptor(ATexture texture);
    void MapTextureToDescriptor(ATexture texture);

    void EnsureDefaultImagesExists(IAjivaEcs ecs);

}
