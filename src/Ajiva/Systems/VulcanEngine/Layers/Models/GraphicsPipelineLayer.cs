﻿using SharpVk;

namespace Ajiva.Systems.VulcanEngine.Layers.Models;

public class GraphicsPipelineLayer : DisposingLogger
{
    public GraphicsPipelineLayer(RenderPassLayer parent, Pipeline pipeline, PipelineLayout pipelineLayout, DescriptorPool descriptorPool, DescriptorSet descriptorSet, DescriptorSetLayout descriptorSetLayout)
    {
        Pipeline = pipeline;
        PipelineLayout = pipelineLayout;
        DescriptorPool = descriptorPool;
        DescriptorSet = descriptorSet;
        DescriptorSetLayout = descriptorSetLayout;
        Parent = parent;
    }

    public Pipeline Pipeline { get; init; }
    public PipelineLayout PipelineLayout { get; init; }
    public DescriptorPool DescriptorPool { get; init; }
    public DescriptorSet DescriptorSet { get; init; }
    public DescriptorSetLayout DescriptorSetLayout { get; init; }
    public RenderPassLayer Parent { get; }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        PipelineLayout.Dispose();
        Pipeline.Dispose();
        DescriptorSetLayout.Dispose();
        DescriptorPool.Dispose();
    }
}