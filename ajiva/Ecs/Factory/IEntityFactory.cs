using System;
using ajiva.Ecs.Entity;

namespace ajiva.Ecs.Factory
{
    public interface IEntityFactory : IDisposable
    {
        IEntity Create(AjivaEcs system, uint id);
    }
    public interface IEntityFactory<T> : IEntityFactory where T : class, IEntity
    {
        new T Create(AjivaEcs system, uint id);
    }
}
