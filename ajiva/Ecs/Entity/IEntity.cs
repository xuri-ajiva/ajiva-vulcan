using System;
using System.Collections.Generic;
using ajiva.Ecs.Component;

namespace ajiva.Ecs.Entity
{
    public interface IEntity: IDisposable
    {
        uint Id { get; init; }

        IDictionary<Type, IComponent> Components { get; }

        bool TryGetComponent<T>(out T? value) where T : class, IComponent;
        T? GetComponent<T>() where T : class, IComponent;
        bool HasComponent<T>() where T : class, IComponent;
        bool HasUpdate { get; }
        void Update(TimeSpan delta);
    }
}
