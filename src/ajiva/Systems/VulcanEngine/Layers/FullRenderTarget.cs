using ajiva.Components;
using ajiva.Systems.VulcanEngine.Layers.Models;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Layers;

public class FullRenderTarget : DisposingLogger
{
    public FullRenderTarget(SwapChainLayer swapChainLayer, Framebuffer[] frameBuffers, RenderPassLayer renderPassLayer, GraphicsPipelineLayer graphicsPipelineLayer, Shader mainShader, uint version)
    {
        SwapChainLayer = swapChainLayer;
        FrameBuffers = frameBuffers;
        RenderPassLayer = renderPassLayer;
        GraphicsPipelineLayer = graphicsPipelineLayer;
        MainShader = mainShader;
        Version = version;
    }

    public SwapChainLayer SwapChainLayer { get; set; }
    public Framebuffer[] FrameBuffers { get; set; }
    public RenderPassLayer RenderPassLayer { get; set; }
    public GraphicsPipelineLayer GraphicsPipelineLayer { get; set; }
    public Shader MainShader { get; set; }
    public uint Version { get; set; }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        base.ReleaseUnmanagedResources(disposing);
        GraphicsPipelineLayer.Dispose();
        MainShader.Dispose();
        foreach (var frameBuffer in FrameBuffers)
            frameBuffer.Dispose();
        RenderPassLayer.Dispose();
        SwapChainLayer.Dispose();
    }
}