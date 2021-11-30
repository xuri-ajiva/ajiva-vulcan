using System;
using System.Collections.Generic;
using System.Linq;

namespace ajiva.Ecs.Utils;

public class AjivaEcsObjectContainer<TResolvingConstrain> : DisposingLogger, IAjivaEcsObjectContainer<TResolvingConstrain> where TResolvingConstrain : class
{
    private List<IAjivaEcsResolver> Resolvers { get; } = new();

    private Dictionary<Type, object> Values { get; } = new();

    /// <inheritdoc />
    public T Add<T, TAs>(T? value = default) where T : class, TResolvingConstrain where TAs : TResolvingConstrain
    {
        value ??= Create<T>();
        Add(typeof(T), value);
        Add(typeof(TAs), value);
        return value;
    }

    /// <inheritdoc />
    public void Add(Type type, TResolvingConstrain value)
    {
        foreach (var ajivaEcsResolver in Resolvers)
        {
            foreach (var resolveA in ajivaEcsResolver.ResolveAs(type))
            {
                Values.TryAdd(resolveA, value);
            }
        }

        Values.TryAdd(type, value);
    }

    /// <inheritdoc />
    public TAs Get<T, TAs>() where TAs : TResolvingConstrain where T : class, TResolvingConstrain
    {
        // ReSharper disable once InvertIf
        if (!Values.ContainsKey(typeof(T)))
        {
            foreach (var resolver in Resolvers)
            {
                if (!resolver.TryResolve<T>(out var value)) continue;
                Add<T, T>(value);
                break;
            }
        }
        return (TAs)Values[typeof(T)];
    }

    /// <inheritdoc />
    public TAs Get<TAs>(Type type) where TAs : TResolvingConstrain
    {
        // ReSharper disable once InvertIf
        if (!Values.ContainsKey(type))
        {
            foreach (var resolver in Resolvers)
            {
                if (!resolver.TryResolve(type, out var value)) continue;
                Values.Add(type, value);
                break;
            }
            return (TAs)Values[type];
        }
        return (TAs)Values[type];
    }

    /// <inheritdoc />
    public TAs GetAny<TAs>(Type type) where TAs: IAjivaEcsObject
    {
        if (Values.ContainsKey(type))
            return (TAs)Values[type];

        foreach (var (typed, value) in Values)
        {
            if (typed.IsAssignableTo(type))
            {
                return (TAs)value;
            }
        }
        throw new KeyNotFoundException();
    }

    /// <inheritdoc />
    public void AddResolver<T>() where T : class, IAjivaEcsResolver, new()
    {
        Resolvers.Add(new T());
    }

    /// <inheritdoc />
    public T Create<T>() where T : class
    {
        return (T)Inject(typeof(T));
    }

    /// <inheritdoc />
    public object Inject(Type type)
    {
        var constructors = type.GetConstructors();
        if (constructors.Length >= 1)
        {
            var parameters = constructors[0].GetParameters();
            var parameterValues = new object?[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (Values.Keys.FirstOrDefault(x => x.IsAssignableTo(parameter.ParameterType)) is { } key)
                {
                    parameterValues[i] = Values[key];
                }
                else
                {
                    foreach (var resolver in Resolvers)
                    {
                        if (resolver.TryResolve(parameter.ParameterType, out var resolved))
                        {
                            parameterValues[i] = resolved;
                        }
                    }
                }
                if (parameterValues[i] is null)
                {
                    throw new ArgumentException("Resolve Failed!");
                }
            }

            var result = constructors[0].Invoke(parameterValues);

            Add(type, (TResolvingConstrain)result);
            foreach (var resolvedType in Resolvers.SelectMany(x => x.ResolveAs(type)))
            {
                Add(resolvedType, (TResolvingConstrain)result);
            }
            return result;
        }
        throw new ArgumentException("No Constructor Found!");
        return null;
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        //todo resolve deps first
        foreach (var value in Values)
        {
            if (value.Value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        base.ReleaseUnmanagedResources(disposing);
    }

    public IEnumerable<T> GetAllAssignableTo<T>()
    {
        return Values.Where(x => x.Value.GetType().IsAssignableTo(typeof(T))).Select(x => x.Value).Distinct().Cast<T>().ToList();
    }
}
public interface IAjivaEcsObjectContainer<in TEcsType> where TEcsType : notnull
{
    T Add<T, TAs>(T? value = default) where TAs : TEcsType where T : class, TEcsType;
    void Add(Type type, TEcsType value);

    T Get<T>() where T : class, TEcsType => Get<T, T>();
    TAs Get<T, TAs>() where TAs : TEcsType where T : class, TEcsType;
    TAs Get<TAs>(Type type) where TAs : TEcsType;

    public TAs GetAny<TAs>(Type type) where TAs: IAjivaEcsObject;
    void AddResolver<T>() where T : class, IAjivaEcsResolver, new();

    T Create<T>() where T : class;
    object Inject(Type type);
}
public static class IAjivaEcsObjectContainerExtensions
{
    public static T AddTasT<TEcsType, T>(this IAjivaEcsObjectContainer<TEcsType> source, T value) where T : class, TEcsType where TEcsType : notnull
    {
        return source.Add<T, T>(value);
    }

    public static T AddTasT<TEcsType, T>(this IAjivaEcsObjectContainer<TEcsType> source) where T : class, TEcsType where TEcsType : notnull
    {
        return source.Add<T, T>();
    }
}
