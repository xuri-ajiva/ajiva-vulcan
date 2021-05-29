using System.Collections.Generic;
using ajiva.Ecs;
using ajiva.Ecs.System;

namespace ajiva.Systems.VulcanEngine.Layer
{
    public class LayerSystem : SystemBase
    {
        public Dictionary<AjivaVulkanPipeline, IAjivaLayer> Layers { get; set; } = new();

        /// <inheritdoc />
        public LayerSystem(AjivaEcs ecs) : base(ecs)
        {
        }

        public void AddUpdateLayer(IAjivaLayer layer)
        {
            Layers[layer.PipelineLayer] = layer;
        }
    }
}
