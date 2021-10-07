using System.Runtime.InteropServices;
using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Ecs.Factory;
using ajiva.Entities;
using SharpVk.NVidia;

namespace ajiva.Factories
{
    public class RectFactory : EntityFactoryBase<Rect>
    {
        /// <inheritdoc />
        public override Rect Create(IAjivaEcs system, uint id)
        {
            var rect = new Rect{Id = id};
            system.TryAttachNewComponentToEntity<Transform2d>(rect, out _);
            if (system.TryAttachNewComponentToEntity<RenderMesh2D>(rect,out var renderMesh))
            {
                renderMesh.Render = true;
                renderMesh.SetMesh(MeshPrefab.Rect);
            }
            //system.AttachComponentToEntity<ATexture>(cube);
            return rect;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            
        }
    }
}
