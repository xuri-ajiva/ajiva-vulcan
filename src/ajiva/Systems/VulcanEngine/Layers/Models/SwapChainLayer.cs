using ajiva.Components.Media;
using ajiva.Models;
using SharpVk;
using SharpVk.Khronos;

namespace ajiva.Systems.VulcanEngine.Layers.Models;

public class SwapChainLayer : DisposingLogger

{
    public SwapChainLayer(Format swapChainFormat, Canvas canvas, Swapchain swapChain, AImage[] swapChainImages)
    {
        SwapChainFormat = swapChainFormat;
        Canvas = canvas;
        SwapChain = swapChain;
        SwapChainImages = swapChainImages;
    }

    public Format SwapChainFormat { get; }
    public Canvas Canvas { get; }
    public Swapchain SwapChain { get; }
    public AImage[] SwapChainImages { get; }
    public List<RenderPassLayer> Children { get; } = new List<RenderPassLayer>();

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        foreach (var child in Children) child.Dispose();
        SwapChain.Dispose();
        foreach (var aImage in SwapChainImages) aImage.Dispose();
    }

    public void AddChild(RenderPassLayer renderPassLayer)
    {
        Children.Add(renderPassLayer);
    }
}