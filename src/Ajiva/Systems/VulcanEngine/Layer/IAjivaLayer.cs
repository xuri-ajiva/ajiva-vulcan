using Ajiva.Models.Buffer.ChangeAware;
using Ajiva.Systems.VulcanEngine.Layers.Models;
using SharpVk;

namespace Ajiva.Systems.VulcanEngine.Layer;

public interface IAjivaLayer<T> : IAjivaLayer, IDisposable where T : unmanaged
{
    new List<IAjivaLayerRenderSystem<T>> LayerRenderComponentSystems { get; }
    public IAChangeAwareBackupBufferOfT<T> LayerUniform { get; }
}
public static class AjivaLayerExtensions
{
    public static void AddLayer<T>(this IAjivaLayer<T> AjivaLayer, IAjivaLayerRenderSystem<T> AjivaLayerRenderSystem) where T : unmanaged
    {
        AjivaLayerRenderSystem.AjivaLayer = AjivaLayer;
        AjivaLayer.LayerRenderComponentSystems.Add(AjivaLayerRenderSystem);
        AjivaLayer.LayerChanged.Changed();
    }
}
public interface IAjivaLayer
{
    public Extent2D Extent { get; }
    public IChangingObserver<IAjivaLayer> LayerChanged { get; }
    List<IAjivaLayerRenderSystem> LayerRenderComponentSystems { get; }
    RenderTarget CreateRenderPassLayer(SwapChainLayer swapChainLayer, PositionAndMax layerIndex, PositionAndMax layerRenderComponentSystemsIndex);
}
public struct PositionAndMax
{
    public PositionAndMax(int index, int start, int end)
    {
        Index = index;
        Start = start;
        End = end;
    }

    public bool First => Index == Start;
    public bool Last => Index == End;
    public int Index { get; init; }
    public int Start { get; init; }
    public int End { get; init; }
}