using Ajiva.Systems.VulcanEngine.Layers.Models;
using SharpVk;

namespace Ajiva.Systems.VulcanEngine.Layer;

public class RenderTarget
{
    public RenderPassLayer PassLayer { get; set; }
    public FrameViewPortInfo ViewPortInfo { get; set; }
    public GraphicsPipelineLayer GraphicsPipelineLayer { get; set; }
    public ClearValue[] ClearValues { get; set; }
}