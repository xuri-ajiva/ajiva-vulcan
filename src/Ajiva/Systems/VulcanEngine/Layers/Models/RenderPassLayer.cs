using SharpVk;

namespace Ajiva.Systems.VulcanEngine.Layers.Models;

public class RenderPassLayer : DisposingLogger
{
    /// <inheritdoc />
    public RenderPassLayer(SwapChainLayer parent, RenderPass renderPass)
    {
        RenderPass = renderPass;
        Parent = parent;
    }

    public RenderPass RenderPass { get; }

    public List<GraphicsPipelineLayer> Children { get; } = new List<GraphicsPipelineLayer>();
    public SwapChainLayer Parent { get; }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        foreach (var child in Children) child.Dispose();
        RenderPass.Dispose();
    }

    public void AddChild(GraphicsPipelineLayer graphicsPipelineLayer)
    {
        Children.Add(graphicsPipelineLayer);
    }
}