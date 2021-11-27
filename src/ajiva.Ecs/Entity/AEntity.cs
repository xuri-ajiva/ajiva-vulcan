using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Ajiva.Wrapper.Logger;

namespace ajiva.Ecs.Entity;

public abstract class AEntity : DisposingLogger, IEntity, IFluentEntity<AEntity>
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

    public T AddComponent<T, TAs>(T component) where TAs : IComponent where T : class,TAs
    {
        if (HasComponent<T>())
        {
            ALog.Warn($"{Id} already Contains {component}");
            return component;
        }
        Components.Add(typeof(T), component);
        return component;
    }

    /// <inheritdoc />
    public T Get<T>() where T : IComponent
    {
        return (T)Components[typeof(T)];
    }

    /// <inheritdoc />
    public AEntity Register(IAjivaEcs ecs)
    {
        foreach (var (type, component) in Components)
        {
            ecs.RegisterComponent(this, type, component);
        }
        ecs.RegisterEntity(this);
        return this;
    }

    /// <inheritdoc />
    public AEntity Configure<TV>(Action<TV> configuration) where TV : IComponent
    {
        if (TryGetComponent<TV>(out var value))
        {
            configuration.Invoke(value);
        }

        return this;
    }

    /// <inheritdoc />
    public AEntity Unregister(IAjivaEcs ecs)
    {
        foreach (var (type, component) in Components)
        {
            ecs.UnRegisterComponent(this, type, component);
        }
        ecs.TryUnRegisterEntity(this);
        return this;
    }
}
