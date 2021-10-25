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
            var cube = new Cube { Id = id };
            //return cube.Create3DRenderedObject(system);
            system.TryAttachNewComponentToEntity<Transform3d>(cube, out _);
            if (system.TryAttachNewComponentToEntity<RenderMesh3D>(cube, out var renderMesh))
            {
                renderMesh.Render = true;
                renderMesh.SetMesh(MeshPrefab.Cube);
            }
            if (system.TryAttachNewComponentToEntity<TextureComponent>(cube, out var textureComponent))
            {
                textureComponent.TextureId = 1;
            }

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
