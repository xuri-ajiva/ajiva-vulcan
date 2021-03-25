using ajiva.Ecs.Entity;
using ajiva.Utils;

namespace ajiva.Ecs.Factory
{
    public abstract class EntityFactoryBase<T> : DisposingLogger, IEntityFactory<T> where T : class, IEntity
    {
        public abstract T Create(AjivaEcs system, uint id);

        /// <inheritdoc />
        IEntity IEntityFactory.Create(AjivaEcs system, uint id)
        {
            return Create(system, id);
        }
    }
}
