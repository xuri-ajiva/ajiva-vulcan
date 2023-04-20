using ajiva.Application;
using ajiva.Components.Media;
using ajiva.Components.Mesh;
using ajiva.Components.Physics;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Components.Transform.Ui;
using ajiva.Ecs;
using ajiva.Ecs.Component;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;
using ajiva.Ecs.System;
using ajiva.Ecs.Utils;
using ajiva.Generators.Texture;
using ajiva.Models.Layers.Layer2d;
using ajiva.Models.Layers.Layer3d;
using ajiva.Systems;
using ajiva.Systems.Assets;
using ajiva.Systems.Physics;
using ajiva.Systems.VulcanEngine;
using ajiva.Systems.VulcanEngine.Debug;
using ajiva.Systems.VulcanEngine.Interfaces;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layer2d;
using ajiva.Systems.VulcanEngine.Layer3d;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;
using ajiva.Worker;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using GlmSharp;
using SharpVk.Glfw;

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

    public static void AddEngine(this ContainerBuilder containerBuilder)
    {
        containerBuilder
            .RegisterType<ContainerProxy>()
            .AsSelf()
            .As<IEntityRegistry>()
            .SingleInstance();

        containerBuilder.AddSingleSelf(new PeriodicUpdateRunner());

        containerBuilder.Register<Config>(c => Config.Default).AsSelf().SingleInstance();

        var (instance, debugReportCallback) = Statics.CreateInstance(Glfw3.GetRequiredInstanceExtensions());
        containerBuilder.AddSingleSelf(instance);
        containerBuilder.AddSingleSelf(debugReportCallback);

        containerBuilder.AddSystem<DeviceSystem, IDeviceSystem>();

        containerBuilder.AddSingle<MeshPool, IMeshPool>();

        containerBuilder.AddSingle<WorkerPool, IWorkerPool>(new WorkerPool(Environment.ProcessorCount / 2, "AjivaWorkerPool") {
            Enabled = true
        });

        containerBuilder.AddSystem<WindowSystem, IWindowSystem>();
        containerBuilder.AddSingleSelf<BoxTextureGenerator>();

        containerBuilder.AddComponentSystem<TextureSystem, ITextureSystem, TextureComponent>();
        containerBuilder.AddComponentSystem<ImageSystem, IImageSystem, AImage>();
        containerBuilder.AddComponentSystem<PhysicsSystem, PhysicsSystem, PhysicsComponent>();
        containerBuilder.AddSystem<AssetManager, IAssetManager>();
        containerBuilder.AddSingleSelf<TextureCreator>();

        containerBuilder
            .RegisterType<TransformComponentSystem>()
            .As<ITransformComponentSystem>()
            .As<IComponentSystem<Transform3d>>()
            //.As<IComponentSystem<ITransform<vec3, mat4>>>()
            .AsSelf().SingleInstance();
        containerBuilder
            .RegisterType<Transform2dComponentSystem>()
            .As<ITransform2dComponentSystem>()
            .As<IComponentSystem<UiTransform>>()
            //.As<IComponentSystem<ITransform<vec2, mat3>>>()
            .AsSelf().SingleInstance();

        containerBuilder.AddSystem<GraphicsSystem, IGraphicsSystem>();

        containerBuilder.AddSystem<Ajiva3dLayerSystem>();
        containerBuilder.AddSystem<Ajiva2dLayerSystem>();
        containerBuilder.AddComponentSystem<SolidMeshRenderLayer, IAjivaLayerRenderSystem<UniformViewProj3d>, RenderInstanceMesh>();
        containerBuilder.AddComponentSystem<DebugLayer, IAjivaLayerRenderSystem<UniformViewProj3d>, DebugComponent>();
        containerBuilder.AddComponentSystem<Mesh2dRenderLayer, IAjivaLayerRenderSystem<UniformLayer2d>, RenderInstanceMesh2D>();

        containerBuilder.AddComponentSystem<CollisionsComponentSystem, CollisionsComponentSystem, CollisionsComponent>();
        containerBuilder.AddComponentSystem<BoundingBoxComponentsSystem, BoundingBoxComponentsSystem, BoundingBox>()
            .As<IComponentSystem<BoundingBox>>(); //todo

//var s = new BoundingBoxComponentsSystem(null);
//IComponentSystem<BoundingBox> s2 = s;
//IComponentSystem<IBoundingBox> s3 = s;
    }

    public static ContainerProxy CreateBaseLayer(this IContainer container)
    {
        var containerProxy = container.Resolve<ContainerProxy>();
        containerProxy.Container = container;
//Transform3d tst = container.Resolve<Transform3d>();

//var t = container.ResolveUnregistered<Transform3d>(new TypedParameter(typeof(vec3), new vec3(1, 2, 3)));

        var config = container.Resolve<Config>();

        var ajiva3dLayerSystem = container.Resolve<Ajiva3dLayerSystem>();
        var ajiva2dLayerSystem = container.Resolve<Ajiva2dLayerSystem>();
        var solidMeshRenderLayer = container.Resolve<SolidMeshRenderLayer>();
        var debugLayer = container.Resolve<DebugLayer>();
        var rectRender = container.Resolve<Mesh2dRenderLayer>();

        var graphicsSystem = container.Resolve<IGraphicsSystem>();

        graphicsSystem.AddUpdateLayer(ajiva3dLayerSystem);
        graphicsSystem.AddUpdateLayer(ajiva2dLayerSystem);

        ajiva3dLayerSystem.AddLayer(solidMeshRenderLayer);
        ajiva3dLayerSystem.AddLayer(debugLayer);
        ajiva2dLayerSystem.AddLayer(rectRender);

//var collisionsComponentSystem = container.Resolve<CollisionsComponentSystem>();
//var boundingBoxComponentsSystem = container.Resolve<BoundingBoxComponentsSystem>();

        //windowSystem = container.Resolve<IWindowSystem>();

        ajiva3dLayerSystem.Update(new UpdateInfo());
        ajiva3dLayerSystem.MainCamara.Transform3d
            //TODO //BUG this calls into Camera.Get instead of FpsCamera.Get if we dont cast
            //BUG it alsow only updates the FPS Camera Transform if we cast, but the Camera Transform is used in the shaders
            .RefPosition((ref vec3 vec) =>
            {
                vec.x = 0;
                vec.y = 0;
                vec.z = -100;
            });

        var meshPool = container.Resolve<IMeshPool>();
        meshPool.AddMesh(MeshPrefab.Cube);
        meshPool.AddMesh(MeshPrefab.Rect);

        return containerProxy;
    }
}
