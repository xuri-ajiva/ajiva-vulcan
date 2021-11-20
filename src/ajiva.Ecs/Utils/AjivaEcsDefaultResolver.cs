using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ajiva.Ecs.Utils;

public class AjivaEcsDefaultResolver : IAjivaEcsResolver
{
    /// <inheritdoc />
    public bool TryResolve(Type type, [MaybeNullWhen(false)] out object value)
    {
        value = null;
        return false;
    }

    /// <inheritdoc />
    public bool TryResolve<T>([MaybeNullWhen(false)] out T value)
    {
        value = default;
        return false;
    }

    /// <inheritdoc />
    public IEnumerable<Type> ResolveAs<T>()
    {
        if (typeof(T).IsAssignableTo(typeof(IComponentSystem<>)))
        {
            yield return typeof(T).GetInterfaces().Where(x => x == typeof(IComponentSystem<>)).FirstOrDefault();
        }
    }

    /// <inheritdoc />
    public IEnumerable<Type> ResolveAs(Type type)
    {
        foreach (var tpe in type.GetInterfaces().Where(x => x.IsGenericType && (x.IsAssignableTo(typeof(IComponentSystem)) || x.IsAssignableTo(typeof(IEntityFactory)))))
        {
            foreach (var typeArgument in tpe.GenericTypeArguments)
            {
                yield return typeArgument;
            }
        }
    }
}
