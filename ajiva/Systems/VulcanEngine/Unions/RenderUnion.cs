using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Models.Buffer;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;
using SharpVk;
using SharpVk.Khronos;

namespace ajiva.Systems.VulcanEngine.Unions
{
    public class RenderUnion : DisposingLogger
    {
        public readonly SwapChainUnion swapChainUnion;
        public readonly Dictionary<AjivaEngineLayer, PipelineFrameUnion> Unions;

        public Semaphore ImageAvailable { get; }
        public Semaphore RenderFinished { get; }

        /// <inheritdoc />
        public RenderUnion(SwapChainUnion swapChainUnion, Semaphore imageAvailable, Semaphore renderFinished, Dictionary<AjivaEngineLayer, PipelineFrameUnion> unions)
        {
            this.swapChainUnion = swapChainUnion;
            Unions = unions;

            ImageAvailable = imageAvailable;
            RenderFinished = renderFinished;
        }

        public static RenderUnion CreateRenderUnion(PhysicalDevice physicalDevice, Device device, Surface surface, Extent2D surfaceExtent, ShaderSystem system, DescriptorImageInfo[] textureSamplerImageViews, bool useDepthImage, AImage depthImage, CommandPool commandPool)
        {
            var swap = SwapChainUnion.CreateSwapChainUnion(physicalDevice, device, surface, surfaceExtent);
            var graph3d = GraphicsPipelineUnion.CreateGraphicsPipelineUnion3D(swap, physicalDevice, device, system, textureSamplerImageViews);
            var graph2d = GraphicsPipelineUnion.CreateGraphicsPipelineUnion2D(swap, physicalDevice, device, system, textureSamplerImageViews);
            var frame3d = FrameBufferUnion.CreateFrameBufferUnion(swap, graph3d, device, useDepthImage, depthImage, commandPool);
            var frame2d = FrameBufferUnion.CreateFrameBufferUnion(swap, graph2d, device, false, depthImage, commandPool);

            var imageAvailable = device.CreateSemaphore()!;
            var renderFinished = device.CreateSemaphore()!;

            return new(swap, imageAvailable, renderFinished, new()
            {
                [AjivaEngineLayer.Layer3d] = new(graph3d, frame3d),
                [AjivaEngineLayer.Layer2d] = new(graph2d, frame2d),
            });
        }

        private void FillBuffer(CommandBuffer buffer, Framebuffer framebuffer, GraphicsPipelineUnion graphicsPipelineUnion, IEnumerable<ARenderAble> renders, ClearValue[] clearValues)
        {
            buffer.Reset();

            buffer.Begin(CommandBufferUsageFlags.SimultaneousUse);

            buffer.BeginRenderPass(graphicsPipelineUnion.RenderPass,
                framebuffer,
                new(new(), swapChainUnion.SwapChainExtent),
                clearValues,
                SubpassContents.Inline);

            buffer.BindPipeline(PipelineBindPoint.Graphics, graphicsPipelineUnion.Pipeline);

            foreach (var renderAble in renders)
            {
                if (!renderAble.Render) continue;
                buffer.BindDescriptorSets(PipelineBindPoint.Graphics, graphicsPipelineUnion.PipelineLayout, 0, graphicsPipelineUnion.DescriptorSet, renderAble.Id * (uint)Unsafe.SizeOf<UniformModel>());
                renderAble.BindAndDraw(buffer);
            }

            buffer.EndRenderPass();
            buffer.End();
        }

        private static readonly Dictionary<AjivaEngineLayer, ClearValue[]> ClearValuesMap = new()
        {
            [AjivaEngineLayer.Layer3d] = new ClearValue[]
            {
                new ClearColorValue(.1f, .1f, .1f, .1f),
                new ClearDepthStencilValue(1, 0),
            },
            [AjivaEngineLayer.Layer2d] = Array.Empty<ClearValue>(),
        };

        public void FillFrameBuffers(Dictionary<AjivaEngineLayer, List<ARenderAble>> render)
        {
            lock (bufferLock)
            {
                foreach (var (pipelineName, graphicsFrameUnion) in Unions)
                {
                    if (!render.ContainsKey(pipelineName)) render.Add(pipelineName, new());
                    
                    for (var index = 0; index < graphicsFrameUnion.FrameBuffer.FrameBuffers.Length; index++)
                    {
                        var commandBuffer = graphicsFrameUnion.FrameBuffer.RenderBuffers[index];
                        var framebuffer = graphicsFrameUnion.FrameBuffer.FrameBuffers[index];
                        FillBuffer(commandBuffer, framebuffer, graphicsFrameUnion.PipelineUnion, render[pipelineName], ClearValuesMap[pipelineName]);
                    }
                }
            }
        }

        public void FillFrameBuffers(IEnumerable<ARenderAble> renderAbles)
        {
            var render = renderAbles.Where(able => able.Render)
                .GroupBy(able => able.AjivaEngineLayer, able => able)
                .ToDictionary(names => names.Key, names => names.ToList());

            FillFrameBuffers(render);
        }

        private readonly object bufferLock = new();

        public void DrawFrame(Queue graphicsQueue, Queue presentQueue)
        {
            lock (bufferLock)
            {
                if (Disposed) return;

                CommandBuffer[] buffers = new CommandBuffer[Unions.Count];

                var nextImage = swapChainUnion.SwapChain!.AcquireNextImage(uint.MaxValue, ImageAvailable, null);

                var i = 0;
                foreach (var union in Unions)
                {
                    buffers[i++] = union.Value.FrameBuffer.RenderBuffers[nextImage];
                }

                var si = new SubmitInfo
                {
                    CommandBuffers = buffers,
                    SignalSemaphores = new[]
                    {
                        RenderFinished
                    },
                    WaitDestinationStageMask = new[]
                    {
                        PipelineStageFlags.ColorAttachmentOutput
                    },
                    WaitSemaphores = new[]
                    {
                        ImageAvailable
                    }
                };

                graphicsQueue!.Submit(si, null);

                var result = new Result[1];
                presentQueue.Present(RenderFinished, swapChainUnion.SwapChain, nextImage, result);
                si.SignalSemaphores = null!;
                si.WaitSemaphores = null!;
                si.WaitDestinationStageMask = null;
                si.CommandBuffers = null;
                // ReSharper disable once RedundantAssignment
                result = null;
                // ReSharper disable once RedundantAssignment
                si = default;
            }
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            lock (bufferLock)
            {
                swapChainUnion.Dispose();
                foreach (var (_, union) in Unions)
                {
                    union.Dispose();
                }

                ImageAvailable.Dispose();
                RenderFinished.Dispose();
            }
        }
    }
    public enum AjivaEngineLayer
    {
        Layer3d,
        Layer2d,
    }
}
