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
        void AttachNewComponent(IEntity entity);
    }
    
    
    public interface IComponentSystem<T> : IComponentSystem where T : class, IComponent
    {
        Dictionary<T, IEntity> ComponentEntityMap { get; }
        T CreateComponent(IEntity entity);
    }
}
