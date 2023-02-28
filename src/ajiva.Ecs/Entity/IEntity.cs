using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ajiva.Ecs.Entity;

public interface IEntity : IAjivaEcsObject
{
    Guid Id { get; }
    bool TryGetComponent<T>([MaybeNullWhen(false)] out T value) where T : IComponent;
    bool HasComponent<T>() where T : IComponent;
    T Get<T>() where T : IComponent;
    IEnumerable<IComponent?> GetComponents();
    IEnumerable<Type> GetComponentTypes();
}
