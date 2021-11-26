using System;
using System.Diagnostics.CodeAnalysis;

namespace ajiva.Ecs.Entity;

public interface IEntity : IDisposable, IAjivaEcsObject
{
    uint Id { get; }
    bool TryGetComponent<T>([MaybeNullWhen(false)] out T value) where T : IComponent;
    bool HasComponent<T>() where T : IComponent;
    bool TryRemoveComponent<T>([MaybeNullWhen(false)] out IComponent component) where T : IComponent;
    T AddComponent<T, TAs>(T component) where TAs : IComponent where T : class, TAs;
}
