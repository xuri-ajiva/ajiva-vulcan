using ajiva.Systems.VulcanEngine.Unions;
using ajiva.Utils.Changing;
using SharpVk;

namespace ajiva.Components.RenderAble
{
    public abstract class ARenderAble : ChangingComponentBase
    {
        public uint Id { get; protected init; }
        public bool Render { get; set; }


        public abstract void BindAndDraw(CommandBuffer commandBuffer);
        
        public abstract AjivaEngineLayer AjivaEngineLayer { get; }

        /// <inheritdoc />
        protected ARenderAble() : base(0)
        {
        }
    }
}
