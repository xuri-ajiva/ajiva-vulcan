using System.Collections.Generic;
using ajiva.Components;
using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Systems.VulcanEngine.Unions;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Layer
{
    public interface IAjivaLayer
    {
        AjivaVulkanPipeline PipelineLayer { get; }

        AImage DepthImage { get; set; }
        Shader MainShader { get; set; }
        PipelineDescriptorInfos[] PipelineDescriptorInfos { get; }
        bool DepthEnabled { get; }
        Canvas Canvas { get; set; }
        VertexInputBindingDescription VertexInputBindingDescription { get; }
        VertexInputAttributeDescription[] VertexInputAttributeDescriptions { get; }
        ClearValue[] ClearValues { get; }

        private const int DISPOSE_DALEY = 1000;

        public void ReCreateDepthImage(ImageSystem imageSystem, Format depthFormat, Canvas canvas)
        {
            if (!DepthEnabled) return;
            DepthImage?.DisposeIn(DISPOSE_DALEY);
            DepthImage = imageSystem.CreateManagedImage(depthFormat, ImageAspectFlags.Depth, canvas);
        }

        List<IRenderMesh> GetRenders();
    }
}
