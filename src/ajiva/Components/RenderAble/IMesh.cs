using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace ajiva.Components.RenderAble;

public interface IMesh : IDisposingLogger
{
    uint MeshId { get; set; }
    void Create(DeviceSystem system);


    void Bind(CommandBuffer commandBuffer);

    void DrawIndexed(CommandBuffer commandBuffer);
}