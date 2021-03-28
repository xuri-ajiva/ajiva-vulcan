using ajiva.Components.Media;
using ajiva.Ecs;
using ajiva.Ecs.Factory;
using ajiva.Entities;

namespace ajiva.Factories
{
    public class TransformEntityFactory : EntityFactoryBase<TransFormEntity>
    {
        /// <inheritdoc />
        public override TransFormEntity Create(AjivaEcs system, uint id)
        {
            var entity = new TransFormEntity() {Id = id};
            system.AttachComponentToEntity<Transform3d>(entity);
            return entity;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
        }
    }
}
