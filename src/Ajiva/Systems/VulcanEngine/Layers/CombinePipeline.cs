using Ajiva.Components;
using Ajiva.Systems.Assets;
using Ajiva.Systems.VulcanEngine.Interfaces;
using Ajiva.Systems.VulcanEngine.Layers.Models;
using SharpVk;

namespace Ajiva.Systems.VulcanEngine.Layers;

public class CombinePipeline : DisposingLogger
{
    private readonly AjivaLayerRenderer layerRenderer;
    private readonly ITextureSystem _textureSystem;
    private readonly IAssetManager _assetManager;

    private FullRenderTarget? fullRenderTarget;

    public CombinePipeline(AjivaLayerRenderer layerRenderer, ITextureSystem textureSystem, IAssetManager assetManager)
    {
        this.layerRenderer = layerRenderer;
        _textureSystem = textureSystem;
        _assetManager = assetManager;
    }

    private static DescriptorImageInfo[] CreateDescriptorImageInfo(IReadOnlyList<BasicLayerRenderProvider> dynamicLayerSystemData, ITextureSystem textureSystem)
    {
        var res = new DescriptorImageInfo[dynamicLayerSystemData.Count];
        for (int i = 0; i < res.Length; i++)
        {
            res[i] = new DescriptorImageInfo {
                Sampler = textureSystem.CreateTextureSampler(),
                ImageView = dynamicLayerSystemData[i].RenderTarget.ViewPortInfo.FrameBufferImage.View,
                ImageLayout = ImageLayout.General
            };
        }
        return res;
    }

    public CommandBuffer Combine(IReadOnlyList<BasicLayerRenderProvider> dynamicLayerSystemData, uint nextImage)
    {
        if (fullRenderTarget is null)
        {
            CreateFullRenderTarget(dynamicLayerSystemData);
        }

        return FillBuffer(nextImage, dynamicLayerSystemData);
    }

    private void CreateFullRenderTarget(IReadOnlyList<BasicLayerRenderProvider> dynamicLayerSystemData)
    {
        if (fullRenderTarget is not null) return;

        var deviceSystem = layerRenderer.DeviceSystem;
        var swapChainLayer = layerRenderer.swapChainLayer;
        var renderPass = deviceSystem.Device!.CreateRenderPass(
            new[]
            {
                new AttachmentDescription(AttachmentDescriptionFlags.None,
                    swapChainLayer.SwapChainFormat,
                    SampleCountFlags.SampleCount1,
                    AttachmentLoadOp.Clear,
                    AttachmentStoreOp.Store,
                    AttachmentLoadOp.DontCare,
                    AttachmentStoreOp.DontCare,
                    ImageLayout.Undefined,
                    ImageLayout.PresentSource),
            },
            new SubpassDescription
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachments = new[] { new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal) },
            },
            new[]
            {
                new SubpassDependency
                {
                    SourceSubpass = Constants.SubpassExternal,
                    DestinationSubpass = 0,
                    SourceStageMask = PipelineStageFlags.BottomOfPipe,
                    SourceAccessMask = AccessFlags.MemoryRead,
                    DestinationStageMask = PipelineStageFlags.ColorAttachmentOutput | PipelineStageFlags.EarlyFragmentTests,
                    DestinationAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite | AccessFlags.DepthStencilAttachmentRead,
                    DependencyFlags = DependencyFlags.None,
                },
                new SubpassDependency
                {
                    SourceSubpass = 0,
                    DestinationSubpass = Constants.SubpassExternal,
                    SourceStageMask = PipelineStageFlags.ColorAttachmentOutput | PipelineStageFlags.EarlyFragmentTests,
                    SourceAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite | AccessFlags.DepthStencilAttachmentRead,
                    DestinationStageMask = PipelineStageFlags.BottomOfPipe,
                    DestinationAccessMask = AccessFlags.MemoryRead,
                    DependencyFlags = DependencyFlags.None
                }
            });

        var frameBuffers = swapChainLayer.SwapChainImages.Select(x => deviceSystem.Device.CreateFramebuffer(renderPass,
            new[] { x.View },
            swapChainLayer.Canvas.Width,
            swapChainLayer.Canvas.Height,
            1)).ToArray();
        var renderPassLayer = new RenderPassLayer(swapChainLayer, renderPass);

        var descriptorImageInfos = CreateDescriptorImageInfo(dynamicLayerSystemData, _textureSystem);

        var mainShader = Shader.CreateShaderFrom(_assetManager, "combine", layerRenderer.DeviceSystem, "main");
        System.Diagnostics.Debug.Assert(layerRenderer.DeviceSystem.Device != null, "deviceSystem.Device != null");
        var descriptorSetLayout = layerRenderer.DeviceSystem.Device.CreateDescriptorSetLayout(
            new DescriptorSetLayoutBinding
            {
                Binding = 1,
                DescriptorCount = (uint)descriptorImageInfos.Length,
                DescriptorType = DescriptorType.CombinedImageSampler,
                StageFlags = ShaderStageFlags.Fragment
            });

        var pipelineLayout = layerRenderer.DeviceSystem.Device.CreatePipelineLayout(descriptorSetLayout, null);

        var pipeline = layerRenderer.DeviceSystem.Device.CreateGraphicsPipelines(null, new GraphicsPipelineCreateInfo
        {
            Layout = pipelineLayout,
            RenderPass = renderPassLayer.RenderPass,
            Subpass = 0,
            VertexInputState = new PipelineVertexInputStateCreateInfo(),
            InputAssemblyState = new PipelineInputAssemblyStateCreateInfo { PrimitiveRestartEnable = false, Topology = PrimitiveTopology.TriangleList },
            ViewportState = new PipelineViewportStateCreateInfo
            {
                Viewports = new[] { new Viewport(0, 0, 500, 500, maxDepth: 1, minDepth: 0) },
                Scissors = new[] { new Rect2D(Offset2D.Zero, new Extent2D(500, 500)) }
            },
            RasterizationState = new PipelineRasterizationStateCreateInfo { DepthClampEnable = false, RasterizerDiscardEnable = false, PolygonMode = PolygonMode.Fill, LineWidth = 1, DepthBiasEnable = false },
            MultisampleState = new PipelineMultisampleStateCreateInfo { SampleShadingEnable = false, RasterizationSamples = SampleCountFlags.SampleCount1, MinSampleShading = 1 },
            ColorBlendState = new PipelineColorBlendStateCreateInfo
            {
                Attachments = new[]
                {
                    new PipelineColorBlendAttachmentState
                    {
                        ColorWriteMask = ColorComponentFlags.R
                                         | ColorComponentFlags.G
                                         | ColorComponentFlags.B
                                         | ColorComponentFlags.A,
                        BlendEnable = true,
                        SourceColorBlendFactor = BlendFactor.SourceAlpha,
                        DestinationColorBlendFactor = BlendFactor.OneMinusSourceAlpha,
                        ColorBlendOp = BlendOp.Add,
                        SourceAlphaBlendFactor = BlendFactor.One,
                        DestinationAlphaBlendFactor = BlendFactor.Zero,
                        AlphaBlendOp = BlendOp.Add
                    } 
                },
                LogicOpEnable = false,
                LogicOp = LogicOp.Copy,
                BlendConstants = (0, 0, 0, 0)
            },
            Stages = new[] { mainShader.VertShaderPipelineStageCreateInfo, mainShader.FragShaderPipelineStageCreateInfo },
            DynamicState = new PipelineDynamicStateCreateInfo { DynamicStates = new[] { DynamicState.Viewport, DynamicState.Scissor, } }
        }).Single();

        var descriptorPool = layerRenderer.DeviceSystem.Device.CreateDescriptorPool(1000,
            new DescriptorPoolSize { Type = DescriptorType.CombinedImageSampler, DescriptorCount = (uint)descriptorImageInfos.Length }
        );
        var descriptorSet = layerRenderer.DeviceSystem.Device.AllocateDescriptorSets(descriptorPool, descriptorSetLayout).Single();

        layerRenderer.DeviceSystem.Device.UpdateDescriptorSets(new WriteDescriptorSet
        {
            DestinationSet = descriptorSet,
            DescriptorCount = (uint)descriptorImageInfos.Length,
            DestinationBinding = 1,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DestinationArrayElement = 0,
            ImageInfo = descriptorImageInfos,
        }, null);

        var graphicsPipelineLayer = new GraphicsPipelineLayer(renderPassLayer, pipeline, pipelineLayout, descriptorPool, descriptorSet, descriptorSetLayout);
        renderPassLayer.AddChild(graphicsPipelineLayer);

        fullRenderTarget = new FullRenderTarget(swapChainLayer, frameBuffers, renderPassLayer, graphicsPipelineLayer, mainShader, MakeVersion(dynamicLayerSystemData));
    }

    private static uint MakeVersion(IEnumerable<BasicLayerRenderProvider> dynamicLayerSystemData)
    {
        var init = dynamicLayerSystemData.Aggregate(0xF3E397F, (current, provider) => HashCode.Combine(current, provider.GetHashCode()));

        return unchecked((uint)init);
    }

    private RenderBuffer[] cache = Array.Empty<RenderBuffer>();

    private CommandBuffer FillBuffer(uint nextImage, IReadOnlyCollection<BasicLayerRenderProvider> basicLayerRenderProviders)
    {
        var version = MakeVersion(basicLayerRenderProviders);
        if (cache.Length <= nextImage || cache[nextImage].Version != version)
        {
            Array.Resize(ref cache, (int)nextImage + 1);
            layerRenderer.CommandBufferPool.ReturnBuffer(cache[nextImage]);
            cache[nextImage] = CreateBuffer(nextImage, basicLayerRenderProviders.Count);
            cache[nextImage].Version = version;
        }

        return cache[nextImage].CommandBuffer;
    }

    private RenderBuffer CreateBuffer(uint nextImage, int count)
    {
        System.Diagnostics.Debug.Assert(fullRenderTarget != null, nameof(fullRenderTarget) + " != null");

        var renderBuffer = layerRenderer.CommandBufferPool.GetNewBuffer();

        var commandBuffer = renderBuffer.CommandBuffer;
        commandBuffer.Reset();
        commandBuffer.Begin(CommandBufferUsageFlags.SimultaneousUse);
        commandBuffer.BeginRenderPass(fullRenderTarget.RenderPassLayer.RenderPass, fullRenderTarget.FrameBuffers[nextImage], fullRenderTarget.SwapChainLayer.Canvas.Rect, new ClearValue[] { new ClearColorValue(.1f, .1f, .1f, .1f) }, SubpassContents.Inline);
        commandBuffer.SetViewport(0, new Viewport(0, 0, fullRenderTarget.SwapChainLayer.Canvas.Width, fullRenderTarget.SwapChainLayer.Canvas.Height, 0, 1));
        commandBuffer.SetScissor(0, fullRenderTarget.SwapChainLayer.Canvas.Rect);
        commandBuffer.BindPipeline(PipelineBindPoint.Graphics, fullRenderTarget.GraphicsPipelineLayer.Pipeline);
        commandBuffer.BindDescriptorSets(PipelineBindPoint.Graphics, fullRenderTarget.GraphicsPipelineLayer.PipelineLayout, 0, fullRenderTarget.GraphicsPipelineLayer.DescriptorSet, ArrayProxy<uint>.Null);
        commandBuffer.Draw(6, (uint)count, 0, 0);
        commandBuffer.EndRenderPass();
        commandBuffer.End();

        return renderBuffer;
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        base.ReleaseUnmanagedResources(disposing);
        fullRenderTarget?.Dispose();
        foreach (var renderBuffer in cache)
            layerRenderer.CommandBufferPool.ReturnBuffer(renderBuffer);
    }
}
