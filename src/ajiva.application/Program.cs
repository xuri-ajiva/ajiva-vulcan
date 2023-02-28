// See https://aka.ms/new-console-template for more information
using System.Collections.Concurrent;
using ajiva;
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
using ajiva.Ecs.Utils;
using ajiva.Entities;
using ajiva.Entities.Ui;
using ajiva.Generators.Texture;
using ajiva.Models.Layers.Layer2d;
using ajiva.Models.Layers.Layer3d;
using ajiva.Systems;
using ajiva.Systems.Assets;
using ajiva.Systems.Assets.Contracts;
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
using Ajiva.Wrapper.Logger;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using GlmSharp;
using SharpVk.Glfw;

if (args.Length > 0)
{
    PackAssets();
}

ALog.MinimumLogLevel = ALogLevel.Debug;
ALog.Log(ALogLevel.Info, $"ProcessId: {Environment.ProcessId}");
ALog.Log(ALogLevel.Info, $"Version: {Environment.Version}");
ALog.Log(ALogLevel.Info, $"Is64BitProcess: {Environment.Is64BitProcess}");
ALog.Log(ALogLevel.Info, $"Is64BitOperatingSystem: {Environment.Is64BitOperatingSystem}");
ALog.Log(ALogLevel.Info, $"OSVersion: {Environment.OSVersion}");

ALog.MinimumLogLevel = ALogLevel.Debug;
Glfw3.Init();
var builder = new ContainerBuilder();
//builder.RegisterSource<MySource>();

builder.AddSingle<ContainerProxy, IAjivaEcs>();
builder.AddSingleSelf(new PeriodicUpdateRunner());

builder.Register<Config>(c => Config.Default).AsSelf().SingleInstance();

var (vulcanInstance, debugReportCallback) = Statics.CreateInstance(Glfw3.GetRequiredInstanceExtensions());
builder.AddSingleSelf(vulcanInstance);

builder.AddSystem<DeviceSystem, IDeviceSystem>();

builder.AddSingle<MeshPool, IMeshPool>();

builder.AddSingle<WorkerPool, IWorkerPool>(new WorkerPool(Environment.ProcessorCount / 2, "AjivaWorkerPool") {
    Enabled = true
});

builder.AddSystem<WindowSystem, IWindowSystem>();
builder.AddSingleSelf<BoxTextureGenerator>();

builder.AddComponentSystem<TextureSystem, ITextureSystem, TextureComponent>();
builder.AddComponentSystem<ImageSystem, IImageSystem, AImage>();
builder.AddComponentSystem<PhysicsSystem, PhysicsSystem, PhysicsComponent>();
builder.AddSystem<AssetManager, IAssetManager>();
builder.AddSingleSelf<TextureCreator>();

builder
    .RegisterType<TransformComponentSystem>()
    .As<ITransformComponentSystem>()
    .As<IComponentSystem<Transform3d>>()
    //.As<IComponentSystem<ITransform<vec3, mat4>>>()
    .AsSelf().SingleInstance();
builder
    .RegisterType<Transform2dComponentSystem>()
    .As<ITransform2dComponentSystem>()
    .As<IComponentSystem<UiTransform>>()
    //.As<IComponentSystem<ITransform<vec2, mat3>>>()
    .AsSelf().SingleInstance();

builder.AddSystem<GraphicsSystem, IGraphicsSystem>();

builder.AddSystem<Ajiva3dLayerSystem>();
builder.AddSystem<Ajiva2dLayerSystem>();
builder.AddComponentSystem<SolidMeshRenderLayer, IAjivaLayerRenderSystem<UniformViewProj3d>, RenderInstanceMesh>();
builder.AddComponentSystem<DebugLayer, IAjivaLayerRenderSystem<UniformViewProj3d>, DebugComponent>();
builder.AddComponentSystem<Mesh2dRenderLayer, IAjivaLayerRenderSystem<UniformLayer2d>, RenderInstanceMesh2D>();

builder.AddComponentSystem<CollisionsComponentSystem, CollisionsComponentSystem, CollisionsComponent>();
builder.AddComponentSystem<BoundingBoxComponentsSystem, BoundingBoxComponentsSystem, BoundingBox>()
    ; // .As<IComponentSystem<BoundingBox>>();  //todo

var container = builder.Build();
var proxy = container.Resolve<ContainerProxy>();
proxy.Container = container;
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

var window = container.Resolve<IWindowSystem>();

var meshPref = MeshPrefab.Cube;

ajiva3dLayerSystem.Update(new UpdateInfo());
ajiva3dLayerSystem.MainCamara.Get<Transform3d>()
    .Position = new vec3(0, 0, -100);

var meshPool = container.Resolve<IMeshPool>();
meshPool.AddMesh(MeshPrefab.Cube);
meshPool.AddMesh(MeshPrefab.Rect);
var leftScreen = new Panel(new UiTransform(null,
    new UiAnchor(UiAlignment.CenterVertical, 0, 1.0f),
    new UiAnchor(UiAlignment.Left, .1f, 0.4f)
)).Register(container);

/*var rect = new Rect().Configure<Rect, RenderMesh3D>(x =>
{
    x.SetMesh(MeshPrefab.Rect);
    x.Render = true;
}).Configure<Rect, UiTransform>(x =>
{
    x.VerticalAnchor = new UiAnchor(UiAlignment.CenterVertical, UiValueUnit.Pixel(20), UiValueUnit.Pixel(100));
    x.HorizontalAnchor = new UiAnchor(UiAlignment.Right, UiValueUnit.Pixel(20), UiValueUnit.Pixel(100));
}).Register(entityComponentSystem);*/

var spinner = new Rect().Configure<RenderInstanceMesh2D>(x =>
    {
        //mesh set in ctor
        /*x.SetMesh(MeshPrefab.Rect);
        x.Render = true;*/
    })
    .Configure<UiTransform>(x =>
    {
        x.VerticalAnchor = new UiAnchor(UiAlignment.CenterVertical, 20, 10);
        x.HorizontalAnchor = new UiAnchor(UiAlignment.CenterHorizontal, 20, 10);
    })
    .Register(container);

//leftScreen.AddChild(rect.Get<UiTransform>());
const int posRange = 20;

for (var i = 0; i < 10; i++)
{
    //BUG: If we configure before register the data is not uploaded properly
    var cube = proxy.CreateAndRegisterEntity<Cube>()
        .Configure<Transform3d>(trans =>
        {
            trans.Position = new vec3(Random.Shared.Next(-posRange, posRange), Random.Shared.Next(-posRange, posRange), Random.Shared.Next(-posRange, posRange));
            trans.Rotation = new vec3(Random.Shared.Next(0, 100), Random.Shared.Next(0, 100), Random.Shared.Next(0, 100));
        });
}

window.OnKeyEvent += WindowOnOnKeyEvent;
var updateRunner = container.Resolve<PeriodicUpdateRunner>();

var types = container.ComponentRegistry.Registrations
    .Where(r => typeof(IUpdate).IsAssignableFrom(r.Activator.LimitType))
    .Select(r => r.Activator.LimitType);

IEnumerable<IUpdate> lst = types.Select(t => container.Resolve(t) as IUpdate);

foreach (var registration in lst.Distinct())
{
    updateRunner.RegisterUpdate(registration);
}
foreach (var update in proxy._updates)
{
    updateRunner.RegisterUpdate(update);
}

updateRunner.Start();
await updateRunner.WaitHandle(LogStatus, CancellationToken.None);

container.Resolve<IDeviceSystem>().WaitIdle();

container.Dispose();

debugReportCallback.Dispose();
vulcanInstance.Dispose();
debugReportCallback = null!;
vulcanInstance = null!;
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();

async void PackAssets()
{
    await AssetPacker.Pack(Const.Default.AssetsFile,
        new AssetSpecification(Const.Default.AssetsPath,
            new Dictionary<AssetType, string> {
                [AssetType.Shader] = "Shaders",
                [AssetType.Texture] = "Textures",
                [AssetType.Model] = "Models"
            }), true);
}

void LogStatus(Dictionary<IUpdate, PeriodicUpdateRunner.UpdateData> updateDatas)
{
    ALog.Info($"PendingWorkItemCount: {ThreadPool.PendingWorkItemCount}, EntitiesCount: {proxy.Entities.Count}");
    ALog.Info(new string('-', 100));
    foreach (var (key, value) in updateDatas)
    {
        ALog.Info($"[ITERATION:{value.Iteration:X8}] | {value.Iteration.ToString(),-8}| {key.GetType().Name,-40}: Delta: {new TimeSpan(value.Delta):G}");
    }
}

void WindowOnOnKeyEvent(object? sender, Key key, int scancode, InputAction inputaction, Modifier modifiers)
{
    if (inputaction != InputAction.Press) return;
    // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
    var meshPref = MeshPrefab.Cube;
    var wp = container.Resolve<IWorkerPool>();

    if (key is > Key.Num0 and <= Key.Num9)
    {
        var index = key - Key.Num0 + 1;
        var rep = 1 << index;
        const float sz = .5f;
        wp.EnqueueWork((info, param) =>
        {
            using var change = container.Resolve<IGraphicsSystem>().ChangingObserver.BeginBigChange();

            var res = Parallel.For(0, rep, new ParallelOptions {
                MaxDegreeOfParallelism = Environment.ProcessorCount / 2
            }, i =>
            {
                for (var j = 0; j < rep; j++)
                {
                    var cube = proxy.CreateAndRegisterEntity<Cube>()
                        .Configure<Transform3d>(trans =>
                        {
                            //trans.Position = new vec3(i * sz, (-index * 2) * Math.Min(rep / (float)i, 10) , j * sz);
                            trans.Position = new vec3(i * sz, (-index * 2), j * sz);
                            trans.Rotation = new vec3(i * 90, j * 90, 0);
                            trans.Scale = new vec3(sz / 2.1f);
                        });
                }
            });
            while (!res.IsCompleted) Task.Delay(10);

            change.Dispose();
            return WorkResult.Succeeded;
        }, o => ALog.Error(o), $"Creation of {rep * rep} Cubes");
    }

    switch (key)
    {
        case Key.Q:
            Task.Run(() => container.Resolve<IGraphicsSystem>().UpdateGraphicsData());
            break;
        case Key.B:
            {
                using var change = container.Resolve<GraphicsSystem>().ChangingObserver.BeginBigChange();

                for (var i = 0; i < 1000; i++)
                {
                    var cube = proxy.CreateAndRegisterEntity<Cube>()
                        .Configure<Transform3d>(trans =>
                        {
                            trans.Position = new vec3(Random.Shared.Next(-posRange, posRange), Random.Shared.Next(-posRange, posRange), Random.Shared.Next(-posRange, posRange));
                            trans.Rotation = new vec3(Random.Shared.Next(0, 100), Random.Shared.Next(0, 100), Random.Shared.Next(0, 100));
                        });
                }
                change.Dispose();
                break;
            }

        case Key.R:
            /*var rect = new Rect().Configure<UiTransform>(x =>
            {
                x.Scale = new vec2(.05f);
            }).Register(entityComponentSystem);*/
            spinner.Get<UiTransform>().Rotation += new vec2(0, .1f);
            break;

        case Key.P:
            var bbs = container.Resolve<BoundingBoxComponentsSystem>();
            wp.EnqueueWork((info, param) =>
            {
                bbs.TogglePhysicUpdate();
                return WorkResult.Succeeded;
            }, exception => ALog.Error(exception), "DoPhysicFrame");

            break;
        case Key.T:
            /*foreach (var keyValuePair in entityComponentSystem.Entities)
            {keyValuePair.Value
            }*/
            break;
        case Key.F:
            var sys = container.Resolve<Ajiva3dLayerSystem>();
            var cubex = proxy.CreateAndRegisterEntity<Cube>()
                .Configure<Transform3d>(trans =>
                {
                    trans.Position = sys.MainCamara.Transform.Position + sys.MainCamara.FrontNormalized * 25;
                    trans.Rotation = sys.MainCamara.Transform.Rotation;
                    trans.Scale = new vec3(3);
                })
                .Configure<ICollider>(x => { x.IsStatic = true; })
                .Configure<PhysicsComponent>(x => { x.IsStatic = true; });

            break;
        case Key.G:
            var sys2 = container.Resolve<Ajiva3dLayerSystem>();
            var cubex2 = proxy.CreateAndRegisterEntity<Cube>()
                .Configure<Transform3d>(trans =>
                {
                    trans.Position = sys2.MainCamara.Transform.Position;
                    trans.Scale = new vec3(10);
                })
                .Configure<ICollider>(x => { x.IsStatic = true; })
                .Configure<PhysicsComponent>(x => { x.IsStatic = true; });

            break;
    }
}

public class ContainerProxy : DisposingLogger, IAjivaEcs
{
    public TAs Get<T, TAs>() where T : class, IAjivaEcsObject where TAs : IAjivaEcsObject
    {
        //ALog.Warn($"Obsolete Resolve<{typeof(T).Name},{typeof(TAs).Name}>", 3);
        var t = Container.Resolve<T>();
        if (t is TAs tAs)
            return tAs;
        return Container.Resolve<TAs>();
    }

    /// <inheritdoc />
    public void RegisterEntity<T>(T entity) where T : class, IEntity
    {
        while (!Entities.TryAdd(entity.Id, entity))
        {
            Thread.Yield();
        }
    }

    public bool TryUnRegisterEntity<T>(uint id, out T entity) where T : IEntity
    {
        throw new NotImplementedException();
    }

    public ConcurrentDictionary<Guid, IEntity> Entities { get; } = new();

    /// <inheritdoc />
    public bool TryUnRegisterEntity<T>(T entity) where T : IEntity
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public T RegisterComponent<T>(IEntity entity, Type type, T component) where T : class, IComponent
    {
        return Container.RegisterComponent(entity, type, component);
    }

    /// <inheritdoc />
    public T UnRegisterComponent<T>(IEntity entity, Type type, T component) where T : class, IComponent
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public long EntitiesCount { get; set; }

    /// <inheritdoc />
    public long ComponentsCount { get; set; }

    public IContainer Container { get; set; }

    /// <inheritdoc />
    public void IssueClose()
    {
        throw new NotImplementedException();
    }

    public List<IUpdate> _updates = new();

    /// <inheritdoc />
    public void RegisterUpdate(IUpdate update)
    {
        //throw new NotImplementedException();
        _updates.Add(update);
    }

    /// <inheritdoc />
    public void StartUpdates()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void WaitForExit()
    {
        throw new NotImplementedException();
    }

    public T CreateAndRegisterEntity<T>() where T : class, IEntity
    {
        var entity = Container.ResolveUnregistered<T>();
        entity.Register(Container);
        return entity;
    }
}
public class MySource : IRegistrationSource
{
    /// <inheritdoc />
    public IEnumerable<IComponentRegistration>
        RegistrationsFor(Service service, Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
    {
        var swt = service as IServiceWithType;
        if (swt == null || !typeof(IComponentSystem).IsAssignableFrom(swt.ServiceType))
            yield break;

        var rb = RegistrationBuilder.ForDelegate(swt.ServiceType, (c, p) =>
        {
            var type = swt.ServiceType.GetGenericArguments()[0];
            var target = typeof(IComponentSystem<>).MakeGenericType(type);
            return c.Resolve(target);
        }).CreateRegistration();

        yield return rb;

        // if swt is IComponent then just create a new instance
        if (typeof(IComponent).IsAssignableFrom(swt.ServiceType))
        {
            var rb2 = RegistrationBuilder.ForDelegate(swt.ServiceType, (c, p) => { return Activator.CreateInstance(swt.ServiceType); }).CreateRegistration();

            yield return rb2;
        }
    }

    /// <inheritdoc />
    public bool IsAdapterForIndividualComponents { get; set; }
}
