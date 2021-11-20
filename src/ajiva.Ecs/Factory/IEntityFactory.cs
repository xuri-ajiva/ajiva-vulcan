using System;

namespace ajiva.Ecs.Factory;

public interface IEntityFactory : IDisposable, IAjivaEcsObject
{
    IEntity Create(IAjivaEcs system, uint id);

    void Delete(IAjivaEcs ecs, IEntity entity)
    {
        foreach (var (_, value) in entity.Components)
        {
            ecs.UnRegisterComponent(entity, value);
        }
        entity.Dispose();
    }
}
public interface IEntityFactory<T> : IEntityFactory where T : class, IEntity
{
    new T Create(IAjivaEcs system, uint id);
}
