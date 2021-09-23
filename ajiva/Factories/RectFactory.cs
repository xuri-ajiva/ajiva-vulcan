using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Ecs.Factory;
using ajiva.Entities;

namespace ajiva.Factories
{
    public class RectFactory : EntityFactoryBase<Rect>
    {
        /// <inheritdoc />
        public override Rect Create(AjivaEcs system, uint id)
        {
            var rect = new Rect();
            system.AttachComponentToEntity<RenderMesh2D>(rect);
            system.AttachComponentToEntity<Transform2d>(rect);
            //system.AttachComponentToEntity<ATexture>(cube);
            return rect;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            
        }
    }
}
