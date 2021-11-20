using System;

namespace ajiva.Ecs.Factory;

public interface IEntityFactory : IDisposable, IAjivaEcsObject
{
    IEntity Create(IAjivaEcs system, uint id);
}
public interface IEntityFactory<T> : IEntityFactory where T : class, IEntity
{
    new T Create(IAjivaEcs system, uint id);
}
