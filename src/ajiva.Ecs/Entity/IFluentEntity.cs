using System;

namespace ajiva.Ecs.Entity;

public interface IFluentEntity<out T> : IEntity where T : IFluentEntity<T>
{
    T Register(IAjivaEcs ecs);
    T Configure<TV>(Action<TV> configuration) where TV : IComponent;
    T Unregister(IAjivaEcs ecs);
}
