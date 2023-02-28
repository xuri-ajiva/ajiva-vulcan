﻿using System;
using System.Collections.Generic;
using Ajiva.Wrapper.Logger;

namespace ajiva.Ecs.ComponentSytem;

public abstract class ComponentSystemBase<T> : DisposingLogger, IComponentSystem<T> where T : IComponent
{
    public ComponentSystemBase(IAjivaEcs ecs)
    {
        Ecs = ecs;
    }

    protected IAjivaEcs Ecs { get; }
    public Type ComponentType { get; } = typeof(T);

    /// <inheritdoc />
    public IComponent RegisterComponent(IEntity entity, IComponent component)
    {
        if (component is T cast)
        {
            return ComponentEntityMap.ContainsKey(cast) ? component : RegisterComponent(entity, cast);
        }
        throw new InvalidCastException();
    }

    /// <inheritdoc />
    public IComponent UnRegisterComponent(IEntity entity, IComponent component)
    {
        if (component is T cast)
        {
            return ComponentEntityMap.ContainsKey(cast) ? UnRegisterComponent(entity, cast) : cast;
        }
        throw new InvalidCastException();
    }

    public Dictionary<T, IEntity> ComponentEntityMap { get; private set; } = new();

    /// <inheritdoc />
    public virtual T RegisterComponent(IEntity entity, T component)
    {
        lock (ComponentEntityMap)
            ComponentEntityMap.Add(component, entity);
        return component;
    }

    /// <inheritdoc />
    public virtual T UnRegisterComponent(IEntity entity, T component)
    {
        lock (ComponentEntityMap)
            if (ComponentEntityMap.Remove(component, out var entity1))
            {
                if (entity != entity1)
                {
                    ALog.Error("Removing component not assigned to entity");
                }
            }
        return component;
    }

    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        /* TODO check if ownership of component has fully moved to entity
        foreach (var (component, entity) in ComponentEntityMap)
        {
            entity?.TryRemoveComponent<T>(out var _);
            component?.Dispose();
        }  */
        ComponentEntityMap.Clear();
        ComponentEntityMap = null!;
    }
}
