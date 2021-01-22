using ajiva.Ecs.Factory;

namespace ajiva.Ecs.Example
{
    public class SomeEntityFactory : EntityFactoryBase<SdtEntity>
    {
        /// <inheritdoc />
        public override SdtEntity Create(AjivaEcs system, uint id)
        {
            var ent = new SdtEntity {Id = id};
            system.AttachComponentToEntity<StdComponent>(ent);
            return ent;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            
        }
    }
}
