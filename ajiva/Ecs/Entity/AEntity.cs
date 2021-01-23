using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ajiva.Ecs.Component;
using ajiva.Helpers;

namespace ajiva.Ecs.Entity
{
    public abstract class AEntity : DisposingLogger, IEntity
    {
        public static uint CurrentId { get; [MethodImpl(MethodImplOptions.Synchronized)] private set; }

        /// <inheritdoc />
        public uint Id { get; init; } = CurrentId++;

        private IDictionary<Type, IComponent> Components { get; } = new Dictionary<Type, IComponent>();

        /// <inheritdoc />
        public bool TryGetComponent<T>(out T? value) where T : class, IComponent
        {
            if (Components.TryGetValue(typeof(T), out var tmp))
            {
                value = (T)tmp;
                return true;
            }
            value = default;
            return false;
        }

        public T GetComponent<T>() where T : class, IComponent
        {
            return (T)Components[typeof(T)];
        }

        /// <inheritdoc />
        public bool HasComponent<T>() where T : class, IComponent
        {
            return Components.ContainsKey(typeof(T));
            //return Components.Any(c => c.GetType() == typeof(T));
        }

        public void AddComponent<T>(T component) where T : class, IComponent
        {
            Components.Add(typeof(T), component);
        }

        /// <inheritdoc />
        public void RemoveComponent<T>() where T : class, IComponent
        {
            Components.Remove(typeof(T));
        }

        public bool HasUpdate { get; protected set; }
        public abstract void Update(TimeSpan delta);
    }
}
