using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ajiva.Ecs;

public interface IAjivaEcsResolver
{
    bool TryResolve(Type type, [MaybeNullWhen(false)] out object value);
    bool TryResolve<T>([MaybeNullWhen(false)] out T value);

    IEnumerable<Type> ResolveAs<T>();
    IEnumerable<Type> ResolveAs(Type type);
}
