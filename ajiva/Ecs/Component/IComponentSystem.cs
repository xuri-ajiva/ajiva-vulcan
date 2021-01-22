using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ajiva.Ecs.Entity;

namespace ajiva.Ecs.Component
{
    public interface IComponentSystem : IDisposable
    {
        Type ComponentType { get; }
        object ComponentEntityMap { get; }
        public Task Init(AjivaEcs ecs);
        void Update(TimeSpan delta);
        public object CreateComponent(IEntity entityId);
        void AttachNewComponent(IEntity entity);
    }

    public interface IComponentSystem<T> : IComponentSystem where T : class, IComponent
    {
        new Dictionary<T, IEntity> ComponentEntityMap { get; }
        new T CreateComponent(IEntity entity);
    }
}
