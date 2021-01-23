using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ajiva.Ecs.Entity;
using ajiva.Helpers;

namespace ajiva.Ecs.Component
{
    public abstract class ComponentSystemBase<T> : DisposingLogger, IComponentSystem<T> where T : class, IComponent
    {
        public Type ComponentType { get; } = typeof(T);

        public Dictionary<T, IEntity> ComponentEntityMap { get; private set; } = new();

        /// <inheritdoc />
        public abstract void Update(TimeSpan delta);

        public abstract Task Init(AjivaEcs ecs);

        /// <inheritdoc />
        object IComponentSystem.CreateComponent(IEntity entity) => CreateComponent(entity);

        /// <inheritdoc />
        public abstract void AttachNewComponent(IEntity entity);

        /// <inheritdoc />
        public abstract T CreateComponent(IEntity entity);

        /// <inheritdoc />
        object IComponentSystem.ComponentEntityMap => ComponentEntityMap;
    }
}
