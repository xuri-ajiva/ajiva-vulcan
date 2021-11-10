namespace ajiva.Ecs.Factory;

public abstract class EntityFactoryBase<T> : DisposingLogger, IEntityFactory<T> where T : class, IEntity
{
    public abstract T Create(IAjivaEcs system, uint id);

    /// <inheritdoc />
    IEntity IEntityFactory.Create(IAjivaEcs system, uint id)
    {
        return Create(system, id);
    }
}