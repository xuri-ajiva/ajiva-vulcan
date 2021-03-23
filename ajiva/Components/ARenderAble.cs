using ajiva.Ecs.Component;
using ajiva.Helpers;
using ajiva.Systems.VulcanEngine.Unions;
using SharpVk;

namespace ajiva.Components
{
    public abstract class ARenderAble : DisposingLogger, IComponent
    {
        public uint Id { get; protected init; }
        public bool Render { get; set; }

        /// <inheritdoc />
        public bool Dirty { get; set; }

        public abstract void BindAndDraw(CommandBuffer commandBuffer);
        
        public abstract AjivaEngineLayer AjivaEngineLayer { get; }
    }
}