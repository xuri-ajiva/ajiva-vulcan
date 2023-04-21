using Ajiva.Systems.VulcanEngine.Layer;
using SharpVk;

namespace Ajiva.Systems.VulcanEngine.Interfaces;

public interface IGraphicsSystem : ISystem
{
    IOverTimeChangingObserver ChangingObserver { get; }
    List<IAjivaLayer> Layers { get; }
    Format DepthFormat { get; set; }

    void RecreateCurrentGraphicsLayout();
    void DrawFrame();
    void UpdateGraphicsData();
    void AddUpdateLayer(IAjivaLayer layer);
}
