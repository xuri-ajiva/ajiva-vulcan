using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Ajiva.Wrapper.Logger;

namespace ajiva.Ecs.Entity;

public abstract class AEntity : DisposingLogger, IEntity, IFluentEntity<AEntity>, IFluentEntity
{
    public AEntity()
    {
        lock (CurrentIdLock)
        {
            Id = CurrentId++;
        }
    }

    private static uint CurrentId;
    private static readonly object CurrentIdLock = new();

    /// <inheritdoc />
    public uint Id { get; init; }

    public IDictionary<Type, IComponent> Components { get; } = new Dictionary<Type, IComponent>();

    public bool TryGetComponent<T>([MaybeNullWhen(false)] out T value) where T : IComponent
    {
        if (Components.TryGetValue(typeof(T), out var tmp))
        {
            value = (T)tmp;
            return true;
        }
        value = default;
        return false;
    }

    public bool HasComponent<T>() where T : IComponent
    {
        return Components.ContainsKey(typeof(T));
    }

    public T? RemoveComponent<T>() where T : IComponent
    {
        return Components.Remove(typeof(T), out var component) ? (T)component : default;
    }

    public bool TryRemoveComponent<T>([MaybeNullWhen(false)] out IComponent component) where T : IComponent
    {
        return Components.Remove(typeof(T), out component);
    }

    public T AddComponent<T, TAs>(T component) where TAs : IComponent where T : class, TAs
    {
        if (!HasComponent<TAs>()) Components.TryAdd(typeof(TAs), component);
        else ALog.Warn($"{Id} already Contains {component} As {typeof(TAs)}");

        /*if (!HasComponent<T>()) Components.TryAdd(typeof(T), component);
        else ALog.Warn($"{Id} already Contains {component}");*/
        return component;
    }

    /// <inheritdoc />
    public T Get<T>() where T : IComponent
    {
        return (T)Components[typeof(T)];
    }

    /// <inheritdoc />
    public T GetAny<T>() where T : IComponent
    {
        if (Components.ContainsKey(typeof(T)))
            return Get<T>();

        foreach (var (type, value) in Components)
        {
            if (type.IsAssignableTo(typeof(T)))
            {
                return (T)value;
            }
        }
        throw new KeyNotFoundException();
    }
    

    /// <inheritdoc />
    public IFluentEntity Configure<TComponent>(Action<TComponent> configuration) where TComponent : IComponent
    {
        if (TryGetComponent<TComponent>(out var value))
        {
            configuration.Invoke(value);
        }

        return this;
    }
}
