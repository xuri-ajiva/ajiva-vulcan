using System;
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
    public IComponent RegisterComponent(IEntity entity, IComponent component) => RegisterComponent(entity, (T)component);

    /// <inheritdoc />
    public IComponent UnRegisterComponent(IEntity entity, IComponent component) => UnRegisterComponent(entity, (T)component);

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
        foreach (var (component, entity) in ComponentEntityMap)
        {
            entity?.TryRemoveComponent<T>(out var _);
            component?.Dispose();
        }
        ComponentEntityMap.Clear();
        ComponentEntityMap = null!;
    }
}
