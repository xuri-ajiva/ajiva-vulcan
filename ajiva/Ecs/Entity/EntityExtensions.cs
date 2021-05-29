﻿using ajiva.Ecs.Component;
using ajiva.Utils;

namespace ajiva.Ecs.Entity
{
    public static class EntityExtensions
    {
        public static bool TryGetComponent<T>(this IEntity entity, out T? value) where T : class, IComponent
        {
            if (entity.Components.TryGetValue(UsVc<T>.Key, out var tmp))
            {
                value = (T)tmp;
                return true;
            }
            value = default;
            return false;
        }

        public static T GetComponent<T>(this IEntity entity) where T : class, IComponent
        {
            return (T)entity.Components[UsVc<T>.Key];
        }

        public static bool HasComponent<T>(this IEntity entity) where T : class, IComponent
        {
            return entity.Components.ContainsKey(UsVc<T>.Key);
        }

        public static void AddComponent<T>(this IEntity entity, T component) where T : class, IComponent
        {
            entity.Components.Add(UsVc<T>.Key, component);
        }

        public static void RemoveComponent<T>(this IEntity entity) where T : class, IComponent
        {
            entity.Components.Remove(UsVc<T>.Key);
        }
    }
}