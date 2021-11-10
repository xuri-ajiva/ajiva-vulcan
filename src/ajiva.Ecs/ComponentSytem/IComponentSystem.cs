using System;
using System.Collections.Generic;

namespace ajiva.Ecs.ComponentSytem;

public interface IComponentSystem : ISystem
{
    Type ComponentType { get; }
}
    
    
public interface IComponentSystem<T> : IComponentSystem where T : class, IComponent
{
    Dictionary<T, IEntity> ComponentEntityMap { get; }
    T CreateComponent(IEntity entity);

    T RegisterComponent(IEntity entity, T component);
    T UnRegisterComponent(IEntity entity, T component);
    IEntity DeleteComponent(IEntity entity, T component);
}