using System.Collections.Generic;
using ajiva.Components.Media;
using ajiva.Utils;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Layers.Models
{
    public class RenderPassLayer : DisposingLogger
    {
        public ClearValue[] ClearValues { get; }
        public RenderPass RenderPass { get; }
        public CommandPool CommandPool { get; }
        public Framebuffer[] FrameBuffers { get; }

        public List<GraphicsPipelineLayer> Children { get; } = new();
        public SwapChainLayer Parent { get; }

        /// <inheritdoc />
        public RenderPassLayer(SwapChainLayer parent, RenderPass renderPass, CommandPool commandPool, Framebuffer[] frameBuffers, ClearValue[] clearValues)
        {
            RenderPass = renderPass;
            CommandPool = commandPool;
            FrameBuffers = frameBuffers;
            ClearValues = clearValues;
            Parent = parent;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            foreach (var child in Children)
            {
                child.Dispose();
            }
            RenderPass.Dispose();
            foreach (var frameBuffer in FrameBuffers)
            {
                frameBuffer.Dispose();
            }
        }

        public void AddChild(GraphicsPipelineLayer graphicsPipelineLayer)
        {
            Children.Add(graphicsPipelineLayer);
        }
    }
}
