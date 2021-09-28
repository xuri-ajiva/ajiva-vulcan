using System;
using System.Collections.Generic;
using ajiva.Ecs.Component;
using ajiva.Ecs.Entity;
using ajiva.Ecs.System;

namespace ajiva.Ecs.ComponentSytem
{
    public interface IComponentSystem : ISystem
    {
        Type ComponentType { get; }
    }
    
    
    public interface IComponentSystem<T> : IComponentSystem where T : class, IComponent
    {
        Dictionary<T, IEntity> ComponentEntityMap { get; }
        T CreateComponent(IEntity entity);

        T RegisterComponent(IEntity entity, T component);
        T RemoveComponent(IEntity entity, T component);
    }
}
