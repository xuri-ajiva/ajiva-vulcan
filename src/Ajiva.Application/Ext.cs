using System.Numerics;
using Ajiva.Assets;
using Ajiva.Components.Media;
using Ajiva.Components.Mesh;
using Ajiva.Components.Physics;
using Ajiva.Components.RenderAble;
using Ajiva.Components.Transform;
using Ajiva.Components.Transform.Ui;
using Ajiva.Ecs;
using Ajiva.Generators.Texture;
using Ajiva.Models.Layers.Layer2d;
using Ajiva.Models.Layers.Layer3d;
using Ajiva.Systems;
using Ajiva.Systems.Physics;
using Ajiva.Systems.VulcanEngine;
using Ajiva.Systems.VulcanEngine.Debug;
using Ajiva.Systems.VulcanEngine.Interfaces;
using Ajiva.Systems.VulcanEngine.Layer;
using Ajiva.Systems.VulcanEngine.Layer2d;
using Ajiva.Systems.VulcanEngine.Layer3d;
using Ajiva.Systems.VulcanEngine.Systems;
using Ajiva.Worker;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using SharpVk.Glfw;

namespace Ajiva.Application;

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
            if (component is not null)
                container.RegisterComponent(entity, component.GetType(), component);
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
            .As<IContainerAccessor>()
            .SingleInstance();

        containerBuilder.RegisterType<UpdateManager>()
            .As<IUpdateManager>()
            .As<ILifetimeManager>()
            .AsSelf()
            .SingleInstance();

        containerBuilder.AddSingleSelf(new PeriodicUpdateRunner());

        Glfw3.Init(); // todo: move to vulkan engine
        var (instance, debugReportCallback) = Statics.CreateInstance(Glfw3.GetRequiredInstanceExtensions());
        containerBuilder.AddSingleSelf(instance);
        //containerBuilder.AddSingleSelf(debugReportCallback);

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

        var config = container.Resolve<AjivaConfig>();

        var Ajiva3dLayerSystem = container.Resolve<Ajiva3dLayerSystem>();
        var Ajiva2dLayerSystem = container.Resolve<Ajiva2dLayerSystem>();
        var solidMeshRenderLayer = container.Resolve<SolidMeshRenderLayer>();
        var debugLayer = container.Resolve<DebugLayer>();
        var rectRender = container.Resolve<Mesh2dRenderLayer>();

        var graphicsSystem = container.Resolve<IGraphicsSystem>();

        graphicsSystem.AddUpdateLayer(Ajiva3dLayerSystem);
        graphicsSystem.AddUpdateLayer(Ajiva2dLayerSystem);

        Ajiva3dLayerSystem.AddLayer(solidMeshRenderLayer);
        Ajiva3dLayerSystem.AddLayer(debugLayer);
        Ajiva2dLayerSystem.AddLayer(rectRender);

//var collisionsComponentSystem = container.Resolve<CollisionsComponentSystem>();
//var boundingBoxComponentsSystem = container.Resolve<BoundingBoxComponentsSystem>();

        //windowSystem = container.Resolve<IWindowSystem>();

        Ajiva3dLayerSystem.Update(new UpdateInfo());
        /*Ajiva3dLayerSystem.MainCamara.Transform3d
            //TODO //BUG this calls into Camera.Get instead of FpsCamera.Get if we dont cast
            //BUG it alsow only updates the FPS Camera Transform if we cast, but the Camera Transform is used in the shaders
            .RefPosition((ref Vector3 vec) =>
            {
                vec.X = 0;
                vec.Y = 0;
                vec.Z = -100;
            });*/

        var meshPool = container.Resolve<IMeshPool>();
        meshPool.AddMesh(MeshPrefab.Cube);
        meshPool.AddMesh(MeshPrefab.Rect);

        return containerProxy;
    }
}
