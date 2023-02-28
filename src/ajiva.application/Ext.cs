using ajiva.Ecs.Component;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;
using ajiva.Ecs.System;
using Autofac;
using Autofac.Builder;
using Autofac.Core;

internal static class Ext
{
    public static IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> AddComponentSystem<T, TAs, TComponent>(this ContainerBuilder builder)
        where T : IComponentSystem<TComponent>, TAs where TComponent : IComponent
    {
        return builder.RegisterType<T>()
            .As<IComponentSystem<TComponent>>()
            .As<TAs>()
            .AsSelf()
            .SingleInstance();
    }

    public static ContainerBuilder AddSingleSelf<T>(this ContainerBuilder builder) where T : notnull
    {
        builder.RegisterType<T>().AsSelf().SingleInstance();
        return builder;
    }

    public static ContainerBuilder AddSingle<T, TAs>(this ContainerBuilder builder) where T : TAs where TAs : notnull
    {
        builder.RegisterType<T>().AsSelf().As<TAs>().SingleInstance();
        return builder;
    }

    public static ContainerBuilder AddSingle<T, TAs>(this ContainerBuilder builder, T value) where T : class, TAs where TAs : notnull
    {
        builder.RegisterInstance(value).As<TAs>().AsSelf().SingleInstance();
        return builder;
    }

    public static ContainerBuilder AddSingleSelf<T>(this ContainerBuilder builder, T value) where T : class
    {
        builder.RegisterInstance(value).AsSelf().SingleInstance();
        return builder;
    }

    public static ContainerBuilder AddSystem<T, TAs>(this ContainerBuilder builder) where T : ISystem, TAs where TAs : notnull
    {
        builder.RegisterType<T>().As<TAs>().AsSelf().SingleInstance();
        return builder;
    }

    public static ContainerBuilder AddSystem<T>(this ContainerBuilder builder) where T : ISystem
    {
        builder.RegisterType<T>().AsSelf().SingleInstance();
        return builder;
    }

    public static T Register<T>(this T entity, IContainer container) where T : class, IEntity
    {
        foreach (var component in entity.GetComponents())
        {
            if (component is not null)
            {
                container.RegisterComponent(entity, component.GetType(), component);
            }
        }
        container.Resolve<ContainerProxy>().RegisterEntity(entity);
        //ecs.RegisterEntity(entity);
        return entity;
    }

    public static T RegisterComponent<T>(this IContainer container, IEntity entity, Type type, T component) where T : class, IComponent
    {
        var target = typeof(IComponentSystem<>).MakeGenericType(type);
        ((IComponentSystem)container.Resolve(target)).RegisterComponent(entity, component);
        //container.Resolve<IComponentSystem<T>>().RegisterComponent(entity, component);
        return component;
    }

    public static T ResolveUnregistered<T>(this IComponentContext context, params Parameter[] parameters) where T : notnull
    {
        var scope = context.Resolve<ILifetimeScope>();
        using var innerScope = scope.BeginLifetimeScope(b => b.RegisterType(typeof(T)).ExternallyOwned());

        innerScope.ComponentRegistry.TryGetRegistration(new TypedService(typeof(T)), out var reg);

        return parameters is not null && parameters.Length > 0
            ? innerScope.Resolve<T>(parameters)
            : innerScope.Resolve<T>();
    }
}