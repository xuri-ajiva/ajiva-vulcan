using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ajiva.Ecs.Component;
using ajiva.Utils;

namespace ajiva.Ecs.Entity
{
    public abstract class AEntity : DisposingLogger, IEntity
    {
        public static uint CurrentId { get; [MethodImpl(MethodImplOptions.Synchronized)] private set; }

        /// <inheritdoc />
        public uint Id { get; init; } = CurrentId++;

        private IDictionary<TypeKey, IComponent> Components { get; } = new Dictionary<TypeKey, IComponent>();

        /// <inheritdoc />
        public bool TryGetComponent<T>(out T? value) where T : class, IComponent
        {
            if (Components.TryGetValue(UsVc<T>.Key, out var tmp))
            {
                value = (T)tmp;
                return true;
            }
            value = default;
            return false;
        }

        public T GetComponent<T>() where T : class, IComponent
        {
            return (T)Components[UsVc<T>.Key];
        }

        /// <inheritdoc />
        public bool HasComponent<T>() where T : class, IComponent
        {
            return Components.ContainsKey(UsVc<T>.Key);
            //return Components.Any(c => c.GetType() == typeof(T));
        }

        public void AddComponent<T>(T component) where T : class, IComponent
        {
            Components.Add(UsVc<T>.Key, component);
        }

        /// <inheritdoc />
        public void RemoveComponent<T>() where T : class, IComponent
        {
            Components.Remove(UsVc<T>.Key);
        }
        public abstract void Update(UpdateInfo delta);
    }
}
