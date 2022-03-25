using System;
using System.Collections.Generic;

namespace ajiva.Ecs.Entity;

public static class FluentEntity
{
    public static T Register<T>(this T entity, IAjivaEcs ecs) where T : class, IFluentEntity
    {
        foreach (var (type, component) in entity.Components)
        {
            ecs.RegisterComponent(entity, type, component);
        }
        ecs.RegisterEntity(entity);
        return entity;
    }

    public static T Configure<T, TComponent>(this T entity, Action<TComponent> configuration) where TComponent : IComponent where T : class, IFluentEntity
    {
        // ReSharper disable once RedundantTypeArgumentsOfMethod
        // to not accidentally call the wrong overload
        entity.Configure<TComponent>(configuration);
        return entity;
    }

    public static T Unregister<T>(this T entity, IAjivaEcs ecs) where T : class, IFluentEntity
    {
        foreach (var (type, component) in entity.Components)
        {
            ecs.UnRegisterComponent(entity, type, component);
        }
        ecs.TryUnRegisterEntity(entity);
        return entity;
    }
}
public interface IFluentEntity : IEntity
{
    IDictionary<Type, IComponent> Components { get; }
    IFluentEntity Configure<TComponent>(Action<TComponent> configuration) where TComponent : IComponent;
}
public interface IFluentEntity<out T> : IFluentEntity where T : IFluentEntity<T>
{
}
