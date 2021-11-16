using SharpVk;

namespace ajiva.Systems.VulcanEngine.Layers.Models;

public class RenderPassLayer : DisposingLogger
{
    /// <inheritdoc />
    public RenderPassLayer(SwapChainLayer parent, RenderPass renderPass, Framebuffer[] frameBuffers, ClearValue[] clearValues)
    {
        RenderPass = renderPass;
        FrameBuffers = frameBuffers;
        ClearValues = clearValues;
        Parent = parent;
    }

    public ClearValue[] ClearValues { get; }
    public RenderPass RenderPass { get; }
    public Framebuffer[] FrameBuffers { get; }

    public List<GraphicsPipelineLayer> Children { get; } = new List<GraphicsPipelineLayer>();
    public SwapChainLayer Parent { get; }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        foreach (var child in Children) child.Dispose();
        RenderPass.Dispose();
        foreach (var frameBuffer in FrameBuffers) frameBuffer.Dispose();
    }

    public void AddChild(GraphicsPipelineLayer graphicsPipelineLayer)
    {
        Children.Add(graphicsPipelineLayer);
    }
}