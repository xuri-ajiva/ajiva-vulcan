using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Ecs.Factory;
using ajiva.Entities;
using ajiva.Systems.VulcanEngine.Debug;

namespace ajiva.Factories
{
    public class CubeFactory : EntityFactoryBase<Cube>
    {
        /// <inheritdoc />
        public override Cube Create(IAjivaEcs system, uint id)
        {
            var cube = new Cube();
            //return cube.Create3DRenderedObject(system);
            system.TryAttachNewComponentToEntity<Transform3d>(cube);
            system.TryAttachNewComponentToEntity<RenderMesh3D>(cube);
            system.TryAttachComponentToEntity(cube, new DebugComponent()
            {
                DrawTransform = true,
                DrawWireframe = true,
            });
            //system.AttachComponentToEntity<ATexture>(cube);
            return cube;
        }

        /// <param name="disposing"></param>
        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
        }
    }
}
