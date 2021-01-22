using ajiva.Components;
using ajiva.Ecs;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Factory;
using ajiva.Entitys;

namespace ajiva.Factorys
{
    public class CubeFactory : EntityFactoryBase<Cube>
    {
        /// <inheritdoc />
        public override Cube Create(AjivaEcs system, uint id)
        {
            var cube = new Cube();
            system.AttachComponentToEntity<Transform3d>(cube);
            system.AttachComponentToEntity<ARenderAble>(cube);
            return cube;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            
        }
    }
}
