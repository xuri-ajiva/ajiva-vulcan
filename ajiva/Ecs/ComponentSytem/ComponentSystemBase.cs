using System;
using System.Collections.Generic;
using ajiva.Ecs.Component;
using ajiva.Ecs.Entity;
using ajiva.Helpers;

namespace ajiva.Ecs.ComponentSytem
{
    public abstract class ComponentSystemBase<T> : DisposingLogger, IComponentSystem<T> where T : class, IComponent
    {
        public Type ComponentType { get; } = typeof(T);

        public Dictionary<T, IEntity> ComponentEntityMap { get; private set; } = new();

        protected abstract void Setup();

        public void Setup(AjivaEcs ecs)
        {
            Ecs = ecs;
            Setup();
        }

        protected AjivaEcs Ecs { get; private set; }

        /// <inheritdoc />
        public abstract void AttachNewComponent(IEntity entity);

        /// <inheritdoc />
        public abstract T CreateComponent(IEntity entity);


        protected override void ReleaseUnmanagedResources()
        {
            foreach (var (component, entity) in ComponentEntityMap)
            {
                entity?.RemoveComponent<T>();
                component?.Dispose();
            }
            ComponentEntityMap.Clear();
            ComponentEntityMap = null;
        }
    }
}
