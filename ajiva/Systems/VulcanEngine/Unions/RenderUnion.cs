using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ajiva.Components;
using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Models;
using ajiva.Models.Buffer;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;
using SharpVk;
using SharpVk.Khronos;

namespace ajiva.Systems.VulcanEngine.Unions
{
    public class RenderUnion : DisposingLogger
    {
        public readonly SwapChainUnion swapChainUnion;
        public readonly Dictionary<AjivaVulkanPipeline, PipelineFrameUnion> Unions;

        public Semaphore ImageAvailable { get; }
        public Semaphore RenderFinished { get; }

        /// <inheritdoc />
        public RenderUnion(SwapChainUnion swapChainUnion, Semaphore imageAvailable, Semaphore renderFinished, Dictionary<AjivaVulkanPipeline, PipelineFrameUnion> unions)
        {
            this.swapChainUnion = swapChainUnion;
            Unions = unions;

            ImageAvailable = imageAvailable;
            RenderFinished = renderFinished;
        }

        public static RenderUnion CreateRenderUnion(DeviceSystem deviceSystem, Canvas canvas)
        {
            var swap = SwapChainUnion.CreateSwapChainUnion(deviceSystem.PhysicalDevice!, deviceSystem.Device!, canvas);

            var imageAvailable = deviceSystem.Device!.CreateSemaphore()!;
            var renderFinished = deviceSystem.Device!.CreateSemaphore()!;

            return new(swap, imageAvailable, renderFinished, new());
        }

        public void AddUpdateLayer3D(AjivaVulkanPipeline layer, DeviceSystem deviceSystem, ShaderSystem system, DescriptorImageInfo[] textureSamplerImageViews, bool useDepthImage, AImage depthImage, CommandPool commandPool, Canvas canvas)
        {
            var graph3d = GraphicsPipelineUnion.CreateGraphicsPipelineUnion3D(swapChainUnion, deviceSystem.PhysicalDevice!, deviceSystem.Device!, system, textureSamplerImageViews, canvas);
            var frame3d = FrameBufferUnion.CreateFrameBufferUnion(swapChainUnion, graph3d, deviceSystem.Device!, useDepthImage, depthImage, commandPool, canvas);

            lock (bufferLock)
            {
                Unions[layer] = new(graph3d, frame3d);
            }
        }

        public void AddUpdateLayer2D(AjivaVulkanPipeline layer, DeviceSystem deviceSystem, ShaderSystem system, DescriptorImageInfo[] textureSamplerImageViews, bool useDepthImage, AImage depthImage, CommandPool commandPool, Canvas canvas)
        {
            var graph2d = GraphicsPipelineUnion.CreateGraphicsPipelineUnion2D(swapChainUnion, deviceSystem.PhysicalDevice!, deviceSystem.Device!, system, textureSamplerImageViews, canvas);
            var frame2d = FrameBufferUnion.CreateFrameBufferUnion(swapChainUnion, graph2d, deviceSystem.Device!, useDepthImage, depthImage, commandPool, canvas);
            lock (bufferLock)
            {
                Unions[layer] = new(graph2d, frame2d);
            }
        }

        public void AddUpdateLayer(AjivaVulkanPipeline layer, DeviceSystem deviceSystem, Shader shaderMain, PipelineDescriptorInfos[] pipelineDescriptorInfos, bool useDepthImage, AImage depthImage, Canvas canvas, VertexInputBindingDescription vertexInputBindingDescription, VertexInputAttributeDescription[] vertexInputAttributeDescription)
        {
            var graph2d = GraphicsPipelineUnion.CreateGraphicsPipelineUnion(swapChainUnion, deviceSystem.PhysicalDevice!, deviceSystem.Device!, useDepthImage, vertexInputBindingDescription, vertexInputAttributeDescription, shaderMain, pipelineDescriptorInfos, canvas);
            FrameBufferUnion frame2d = default!;
            deviceSystem.UseCommandPool(commandPool =>
            {
                frame2d = FrameBufferUnion.CreateFrameBufferUnion(swapChainUnion, graph2d, deviceSystem.Device!, useDepthImage, depthImage, commandPool, canvas);
            });
            lock (bufferLock)
            {
                Unions[layer] = new(graph2d, frame2d);
            }
        }

        public void AddUpdateLayer(AjivaVulkanPipeline layer, DeviceSystem deviceSystem, Shader shaderMain, IBufferOfT viewProj, IBufferOfT uniformModels, DescriptorImageInfo[] textureSamplerImageViews, VertexInputBindingDescription vertexInputBindingDescription, VertexInputAttributeDescription[] vertexInputAttributeDescription, bool useDepthImage, AImage depthImage, Canvas canvas)
        {
            AddUpdateLayer(layer, deviceSystem, shaderMain, PipelineDescriptorInfos.CreateFrom(viewProj, uniformModels, textureSamplerImageViews), useDepthImage, depthImage, canvas, vertexInputBindingDescription, vertexInputAttributeDescription);
        }

        public void AddUpdateLayer(IAjivaLayer layer, DeviceSystem deviceSystem)
        {
            AddUpdateLayer(layer.PipelineLayer, deviceSystem, layer.MainShader, layer.PipelineDescriptorInfos, layer.DepthEnabled, layer.DepthImage, layer.Canvas, layer.VertexInputBindingDescription, layer.VertexInputAttributeDescriptions);
            AddUpdateClearValue(layer.PipelineLayer, layer.ClearValues);
        }

        private void AddUpdateClearValue(AjivaVulkanPipeline layer, ClearValue[] clearValues)
        {
            ClearValuesMap[layer] = clearValues;
        }

        private readonly Dictionary<AjivaVulkanPipeline, ClearValue[]> ClearValuesMap = new();

        public void FillFrameBuffers(Dictionary<AjivaVulkanPipeline, List<IRenderMesh>> render, IRenderMeshPool pool)
        {
            lock (bufferLock)
            {
                foreach (var (pipelineName, graphicsFrameUnion) in Unions)
                {
                    if (!render.ContainsKey(pipelineName))
                        render.Add(pipelineName, new()); //todo: can we just continue and leave the old stuff in the buffer

                    FillUnionBuffers(render[pipelineName], ClearValuesMap[pipelineName], graphicsFrameUnion, pool);
                }
            }
        }

        public void FillFrameBuffer(AjivaVulkanPipeline layer, List<IRenderMesh> renders, IRenderMeshPool pool)
        {
            lock (bufferLock)
            {
                FillUnionBuffers(renders, ClearValuesMap[layer], Unions[layer], pool);
            }
        }

        private void FillUnionBuffers(IReadOnlyCollection<IRenderMesh> renders, ClearValue[] clearValues, PipelineFrameUnion graphicsFrameUnion, IRenderMeshPool pool)
        {
            for (var index = 0; index < graphicsFrameUnion.FrameBuffer.FrameBuffers.Length; index++)
            {
                var commandBuffer = graphicsFrameUnion.FrameBuffer.RenderBuffers[index];
                var framebuffer = graphicsFrameUnion.FrameBuffer.FrameBuffers[index];
                FillUnionBuffer(graphicsFrameUnion, commandBuffer, framebuffer, clearValues, renders.OrderBy(x => x.MeshId), pool);
            }
        }

        private void FillUnionBuffer(PipelineFrameUnion graphicsFrameUnion, CommandBuffer commandBuffer, Framebuffer framebuffer, ClearValue[] clearValues, IOrderedEnumerable<IRenderMesh> renders, IRenderMeshPool pool)
        {
            commandBuffer.Reset();

            commandBuffer.Begin(CommandBufferUsageFlags.SimultaneousUse);

            commandBuffer.BeginRenderPass(graphicsFrameUnion.PipelineUnion.RenderPass,
                framebuffer,
                swapChainUnion.Canvas.Rect,
                clearValues,
                SubpassContents.Inline);

            commandBuffer.BindPipeline(PipelineBindPoint.Graphics, graphicsFrameUnion.PipelineUnion.Pipeline);

            pool.Reset();
            foreach (var renderIt in renders)
            {
                if (!renderIt.Render) continue;

                commandBuffer.BindDescriptorSets(PipelineBindPoint.Graphics, graphicsFrameUnion.PipelineUnion.PipelineLayout, 0, graphicsFrameUnion.PipelineUnion.DescriptorSet, renderIt.Id * (uint)Unsafe.SizeOf<UniformModel>());

                pool.DrawMesh(commandBuffer, renderIt.MeshId);
            }

            commandBuffer.EndRenderPass();
            commandBuffer.End();
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
        protected override void ReleaseUnmanagedResources(bool disposing)
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
}
