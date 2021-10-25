using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Ecs.Factory;
using ajiva.Entities;
using ajiva.Systems.VulcanEngine.Debug;

namespace ajiva.Factories
{
    public class DebugBoxFactory : EntityFactoryBase<DebugBox>
    {
        /// <inheritdoc />
        public override DebugBox Create(IAjivaEcs system, uint id)
        {
            var box = new DebugBox { Id = id };
            //return cube.Create3DRenderedObject(system);
            system.TryAttachNewComponentToEntity<Transform3d>(box, out _);
            if (system.TryAttachNewComponentToEntity<DebugComponent>(box, out var debugComponent))
            {
                debugComponent.DrawTransform = true;
                debugComponent.DrawWireframe = true;
                debugComponent.Render = true;
                debugComponent.SetMesh(MeshPrefab.Cube);
            }
            //system.AttachComponentToEntity<ATexture>(cube);
            return box;
        }

        /// <param name="disposing"></param>
        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
        }
    }
}
