﻿using System;
using System.Collections.Generic;
using ajiva.Ecs.Component;
using ajiva.Ecs.Entity;
using ajiva.Utils;
using Ajiva.Wrapper.Logger;

namespace ajiva.Ecs.ComponentSytem
{
    public abstract class ComponentSystemBase<T> : DisposingLogger, IComponentSystem<T> where T : class, IComponent, new()
    {
        public Type ComponentType { get; } = typeof(T);

        public Dictionary<T, IEntity> ComponentEntityMap { get; private set; } = new();

        protected IAjivaEcs Ecs { get; }

        /// <inheritdoc />
        public virtual T CreateComponent(IEntity entity)
        {
            return RegisterComponent(entity, new T()); 
        }

        /// <inheritdoc />
        public virtual T RegisterComponent(IEntity entity, T component)
        {
            ComponentEntityMap.Add(component, entity);
            return component;
        }

        /// <inheritdoc />
        public virtual T UnRegisterComponent(IEntity entity, T component)
        {
            if (ComponentEntityMap.Remove(component, out var entity1))
            {
                if (entity != entity1)
                {
                    LogHelper.Log("Error: Removing component not assigned to entity");
                }
            }
            return component;
        }

        /// <inheritdoc />
        public virtual IEntity DeleteComponent(IEntity entity, T component)
        {
            var cmp = UnRegisterComponent(entity, component);
            cmp?.Dispose();
            return entity;
        }

        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            foreach (var (component, entity) in ComponentEntityMap)
            {
                entity?.RemoveComponent<T>();
                component?.Dispose();
            }
            ComponentEntityMap.Clear();
            ComponentEntityMap = null!;
        }

        public ComponentSystemBase(IAjivaEcs ecs)
        {
            Ecs = ecs;
        }
    }
}