using ajiva.Components;
using ajiva.Ecs;
using ajiva.Ecs.Factory;
using ajiva.Entitys;

namespace ajiva.Factorys
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
        protected override void ReleaseUnmanagedResources()
        {
        }
    }
}