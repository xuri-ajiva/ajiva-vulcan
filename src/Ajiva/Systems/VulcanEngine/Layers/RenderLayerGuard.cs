using Ajiva.Systems.VulcanEngine.Layer;
using Ajiva.Systems.VulcanEngine.Layers.Models;
using SharpVk;

namespace Ajiva.Systems.VulcanEngine.Layers;

public ref struct RenderLayerGuard
{
    public RenderLayerGuard(RenderBuffer renderBuffer, RenderTarget renderTarget, AjivaLayerRenderer renderer)
    {
        RenderBuffer = renderBuffer;
        ClearValues = renderTarget.ClearValues;

        Pipeline = renderTarget.GraphicsPipelineLayer;
        Renderer = renderer;
        RenderPassInfo = renderTarget.PassLayer;
    }

    public RenderBuffer RenderBuffer { get; set; }
    public CommandBuffer Buffer => RenderBuffer.CommandBuffer;
    public GraphicsPipelineLayer Pipeline { get; set; }
    public AjivaLayerRenderer Renderer { get; set; }
    public RenderPassLayer RenderPassInfo { get; set; }
    public ClearValue[] ClearValues { get; set; }

    public void BindDescriptor(ArrayProxy<uint>? dynamicOffsets = null)
    {
        Buffer.BindDescriptorSets(PipelineBindPoint.Graphics, Pipeline.PipelineLayout, 0, Pipeline.DescriptorSet, dynamicOffsets ?? ArrayProxy<uint>.Null);
    }

    public void Capture(object obj)
    {
        RenderBuffer?.Captured.Add(obj);
    }
}