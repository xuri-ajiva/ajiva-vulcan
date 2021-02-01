using ajiva.Components;
using ajiva.Ecs;
using ajiva.Ecs.Factory;
using ajiva.Entities;

namespace ajiva.Factories
{
    public class CubeFactory : EntityFactoryBase<Cube>
    {
        /// <inheritdoc />
        public override Cube Create(AjivaEcs system, uint id)
        {
            var cube = new Cube();
            system.AttachComponentToEntity<Transform3d>(cube);
            system.AttachComponentToEntity<ARenderAble>(cube);
            //system.AttachComponentToEntity<ATexture>(cube);
            return cube;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            
        }
    }
}
