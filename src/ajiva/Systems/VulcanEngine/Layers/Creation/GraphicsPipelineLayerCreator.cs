using ajiva.Components;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Layers.Creation;

public static class GraphicsPipelineLayerCreator
{
    public static GraphicsPipelineLayer Default(SwapChainLayer swapChainLayer, RenderPassLayer renderPassLayer, DeviceSystem deviceSystem, bool useDepthImage, VertexInputBindingDescription[] bindingDescriptions, VertexInputAttributeDescription[] attributeDescriptions, Shader mainShader, PipelineDescriptorInfos[] descriptorInfos)
    {
        System.Diagnostics.Debug.Assert(deviceSystem.Device != null, "deviceSystem.Device != null");
        var descriptorSetLayout = deviceSystem.Device.CreateDescriptorSetLayout(
            descriptorInfos.Select(descriptor => new DescriptorSetLayoutBinding
            {
                Binding = descriptor.DestinationBinding,
                DescriptorCount = descriptor.DescriptorCount,
                DescriptorType = descriptor.DescriptorType,
                StageFlags = descriptor.StageFlags
            }).ToArray());

        var pipelineLayout = deviceSystem.Device.CreatePipelineLayout(descriptorSetLayout, null);

        var pipeline = deviceSystem.Device.CreateGraphicsPipelines(null, new[]
        {
            new GraphicsPipelineCreateInfo
            {
                Layout = pipelineLayout,
                RenderPass = renderPassLayer.RenderPass,
                Subpass = 0,
                VertexInputState = new PipelineVertexInputStateCreateInfo
                {
                    VertexBindingDescriptions = bindingDescriptions,
                    VertexAttributeDescriptions = attributeDescriptions
                },
                InputAssemblyState = new PipelineInputAssemblyStateCreateInfo
                {
                    PrimitiveRestartEnable = false,
                    Topology = PrimitiveTopology.TriangleList
                },
                ViewportState = new PipelineViewportStateCreateInfo
                {
                    Viewports = new[]
                    {
                        new Viewport
                        {
                            X = swapChainLayer.Canvas.Xf,
                            Y = swapChainLayer.Canvas.Yf,
                            Width = swapChainLayer.Canvas.WidthF,
                            Height = swapChainLayer.Canvas.HeightF,
                            MaxDepth = 1,
                            MinDepth = 0
                        }
                    },
                    Scissors = new[]
                    {
                        new Rect2D
                        {
                            Offset = swapChainLayer.Canvas.Offset,
                            Extent = swapChainLayer.Canvas.Extent
                        }
                    }
                },
                RasterizationState = new PipelineRasterizationStateCreateInfo
                {
                    DepthClampEnable = false,
                    RasterizerDiscardEnable = false,
                    PolygonMode = PolygonMode.Fill,
                    LineWidth = 1,
                    //CullMode = CullModeFlags.Back,          // reenable to make faces only visible from one side
                    //FrontFace = FrontFace.CounterClockwise,
                    DepthBiasEnable = false
                },
                MultisampleState = new PipelineMultisampleStateCreateInfo
                {
                    SampleShadingEnable = false,
                    RasterizationSamples = SampleCountFlags.SampleCount1,
                    MinSampleShading = 1
                },
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
                            BlendEnable = false,
                            SourceColorBlendFactor = BlendFactor.One,
                            DestinationColorBlendFactor = BlendFactor.Zero,
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
                Stages = new[]
                {
                    mainShader.VertShaderPipelineStageCreateInfo,
                    mainShader.FragShaderPipelineStageCreateInfo
                },
                DepthStencilState = useDepthImage
                    ? new PipelineDepthStencilStateCreateInfo
                    {
                        DepthTestEnable = true,
                        DepthWriteEnable = true,
                        DepthCompareOp = CompareOp.Less,
                        DepthBoundsTestEnable = false,
                        MinDepthBounds = 0,
                        MaxDepthBounds = 1,
                        StencilTestEnable = false,
                        Back = new StencilOpState(),
                        Flags = new PipelineDepthStencilStateCreateFlags()
                    }
                    : null
            }
        }).Single();

        var descriptorPool = deviceSystem.Device.CreateDescriptorPool(10000,
            descriptorInfos.Select(descriptor => new DescriptorPoolSize
            {
                Type = descriptor.DescriptorType,
                DescriptorCount = descriptor.DescriptorCount
            }).ToArray());
        var descriptorSet = deviceSystem.Device.AllocateDescriptorSets(descriptorPool, descriptorSetLayout).Single();

        deviceSystem.Device.UpdateDescriptorSets(
            descriptorInfos.Select(descriptor => new WriteDescriptorSet
            {
                DestinationSet = descriptorSet,
                DescriptorCount = descriptor.DescriptorCount,
                DestinationBinding = descriptor.DestinationBinding,
                DescriptorType = descriptor.DescriptorType,
                DestinationArrayElement = descriptor.DestinationArrayElement,
                BufferInfo = descriptor.BufferInfo,
                ImageInfo = descriptor.ImageInfo,
                TexelBufferView = descriptor.TexelBufferView
            }).ToArray(), null);

        var graphicsPipelineLayer = new GraphicsPipelineLayer(renderPassLayer, pipeline, pipelineLayout, descriptorPool, descriptorSet, descriptorSetLayout);
        renderPassLayer.AddChild(graphicsPipelineLayer);
        return graphicsPipelineLayer;
    }
}
