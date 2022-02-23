using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Utils.Changing;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Interfaces;

public interface IGraphicsSystem : ISystem
{
    IOverTimeChangingObserver ChangingObserver { get; }
    List<IAjivaLayer> Layers { get; }
    Format DepthFormat { get; set; }

    void RecreateCurrentGraphicsLayout();
    void DrawFrame();
    void ResolveDeps();
    void UpdateGraphicsData();
    void AddUpdateLayer(IAjivaLayer layer);
}
