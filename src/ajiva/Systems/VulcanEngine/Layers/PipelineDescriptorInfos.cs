using ajiva.Models.Buffer;
using ajiva.Models.Layers.Layer3d;
using SharpVk;
using Buffer = SharpVk.Buffer;

namespace ajiva.Systems.VulcanEngine.Layers;

public record PipelineDescriptorInfos(DescriptorType DescriptorType, ShaderStageFlags StageFlags, uint DestinationBinding, uint DescriptorCount, uint DestinationArrayElement = 0, DescriptorImageInfo[]? ImageInfo = default, BufferView[]? TexelBufferView = default, DescriptorBufferInfo[]? BufferInfo = default)
{
    public static PipelineDescriptorInfos[] CreateFrom(UniformBuffer<UniformViewProj3d> viewProj, UniformBuffer<SolidUniformModel> uniformModels, DescriptorImageInfo[] textureSamplerImageViews)
    {
        return new[]
        {
            new PipelineDescriptorInfos(DescriptorType.UniformBuffer, ShaderStageFlags.Vertex, 0, 1, BufferInfo: new[]
            {
                new DescriptorBufferInfo
                {
                    Buffer = viewProj.Uniform.Buffer,
                    Offset = 0,
                    Range = viewProj.Uniform.SizeOfT
                }
            }),
            new PipelineDescriptorInfos(DescriptorType.UniformBufferDynamic, ShaderStageFlags.Vertex, 1, 1, BufferInfo: new[]
            {
                new DescriptorBufferInfo
                {
                    Buffer = uniformModels.Uniform.Buffer,
                    Offset = 0,
                    Range = uniformModels.Uniform.SizeOfT
                }
            }),
            new(DescriptorType.CombinedImageSampler, ShaderStageFlags.Fragment, 2, (uint)textureSamplerImageViews.Length, ImageInfo: textureSamplerImageViews)
        };
    }

    public static PipelineDescriptorInfos[] CreateFrom(IBufferOfT viewProj, IBufferOfT uniformModels, DescriptorImageInfo[] textureSamplerImageViews)
    {
        return CreateFrom(viewProj.Buffer!, viewProj.SizeOfT, uniformModels.Buffer!, uniformModels.SizeOfT, textureSamplerImageViews);
    }

    public static PipelineDescriptorInfos[] CreateFrom(Buffer viewProj, uint sizeOfViewProj, Buffer uniformModels, uint sizeOfModels, DescriptorImageInfo[] textureSamplerImageViews)
    {
        return new[]
        {
            new PipelineDescriptorInfos(DescriptorType.UniformBuffer, ShaderStageFlags.Vertex, 0, 1, BufferInfo: new[]
            {
                new DescriptorBufferInfo
                {
                    Buffer = viewProj,
                    Offset = 0,
                    Range = sizeOfViewProj
                }
            }),
            new PipelineDescriptorInfos(DescriptorType.UniformBufferDynamic, ShaderStageFlags.Vertex, 1, 1, BufferInfo: new[]
            {
                new DescriptorBufferInfo
                {
                    Buffer = uniformModels,
                    Offset = 0,
                    Range = sizeOfModels
                }
            }),
            new(DescriptorType.CombinedImageSampler, ShaderStageFlags.Fragment, 2, (uint)textureSamplerImageViews.Length, ImageInfo: textureSamplerImageViews)
        };
    }
}
