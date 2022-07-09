using ajiva.Components.Mesh;
using ajiva.Components.Physics;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Components.Transform.Ui;
using ajiva.Ecs;
using ajiva.Entities;
using ajiva.Entities.Ui;
using ajiva.Generators.Texture;
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
using ajiva.Worker;
using GlmSharp;
using SharpVk;
using SharpVk.Glfw;
using SharpVk.Multivendor;

namespace ajiva.Application;

public class AjivaApplication : DisposingLogger
{
    private const int size = 100;
    private const int posRange = 20;
    private const float scale = 0.7f;

    private readonly IAjivaEcs entityComponentSystem;
    private DebugReportCallback debugReportCallback;
    private readonly Random r = new Random();

    private Instance vulcanInstance;
    private Rect spinner;

    public AjivaApplication(CancellationTokenSource tokenSource)
    {
        entityComponentSystem = new AjivaEcs(tokenSource);
        entityComponentSystem.AddTasT(entityComponentSystem);
    }

    private bool Running { get; set; }

    public void Run()
    {
        Running = true;
        entityComponentSystem.StartUpdates();
        entityComponentSystem.WaitForExit();
        Running = false;
    }

    public void Init()
    {
        entityComponentSystem.Add<Config, Config>(Config.Default);

        (vulcanInstance, debugReportCallback) = Statics.CreateInstance(Glfw3.GetRequiredInstanceExtensions());
        var deviceSystem = entityComponentSystem.Add<DeviceSystem, IDeviceSystem>();

        var meshPool = new MeshPool(deviceSystem);
        entityComponentSystem.Add<MeshPool, IMeshPool>(meshPool);
        //entityComponentSystem.Add<InstanceMeshPool, IInstanceMeshPool>(instanceMeshPool); // should be unique per instance user

        entityComponentSystem.Add<VulcanInstance, IVulcanInstance>(new VulcanInstance(vulcanInstance));

        entityComponentSystem.Add<WorkerPool, IWorkerPool>(new WorkerPool(Environment.ProcessorCount / 2, "AjivaWorkerPool", entityComponentSystem) { Enabled = true });
        var window = entityComponentSystem.Add<WindowSystem, IWindowSystem>();
        entityComponentSystem.Add<BoxTextureGenerator, BoxTextureGenerator>();

        //var layerSystem = entityComponentSystem.CreateSystemOrComponentSystem<LayerSystem>();

        entityComponentSystem.Add<TextureSystem, ITextureSystem>();
        entityComponentSystem.Add<ImageSystem, IImageSystem>();
        entityComponentSystem.Add<TransformComponentSystem, ITransformComponentSystem>();
        entityComponentSystem.Add<Transform2dComponentSystem, ITransform2dComponentSystem>();
        entityComponentSystem.Add<PhysicsSystem, PhysicsSystem>();
        entityComponentSystem.Add<AssetManager, IAssetManager>();

        var graphicsSystem = entityComponentSystem.Add<GraphicsSystem, IGraphicsSystem>();

        window.OnKeyEvent += WindowOnOnKeyEvent;
        var ajiva3dLayerSystem = entityComponentSystem.Add<Ajiva3dLayerSystem, Ajiva3dLayerSystem>();
        var ajiva2dLayerSystem = entityComponentSystem.Add<Ajiva2dLayerSystem, Ajiva2dLayerSystem>();
        var solidMeshRenderLayer = entityComponentSystem.Add<SolidMeshRenderLayer, SolidMeshRenderLayer>();
        var debugLayer = entityComponentSystem.Add<DebugLayer, DebugLayer>();
        var rectRender = entityComponentSystem.Add<Mesh2dRenderLayer, Mesh2dRenderLayer>();

        var collisionsComponentSystem = entityComponentSystem.Add<CollisionsComponentSystem, CollisionsComponentSystem>();
        var boundingBoxComponentsSystem = entityComponentSystem.Add<BoundingBoxComponentsSystem, BoundingBoxComponentsSystem>();

        graphicsSystem.AddUpdateLayer(ajiva3dLayerSystem);
        graphicsSystem.AddUpdateLayer(ajiva2dLayerSystem);

        ajiva3dLayerSystem.AddLayer(solidMeshRenderLayer);
        ajiva3dLayerSystem.AddLayer(debugLayer);
        ajiva2dLayerSystem.AddLayer(rectRender);

        var meshPref = MeshPrefab.Cube;
        var r = new Random();

        entityComponentSystem.Init();

        meshPool.AddMesh(MeshPrefab.Cube);
        meshPool.AddMesh(MeshPrefab.Rect);
        var leftScreen = new Panel(new UiTransform(null,
            new UiAnchor(UiAlignment.CenterVertical, 0, 1.0f),
            new UiAnchor(UiAlignment.Left, .1f, 0.4f)
        )).Register(entityComponentSystem);

        /*var rect = new Rect().Configure<Rect, RenderMesh3D>(x =>
        {
            x.SetMesh(MeshPrefab.Rect);
            x.Render = true;
        }).Configure<Rect, UiTransform>(x =>
        {
            x.VerticalAnchor = new UiAnchor(UiAlignment.CenterVertical, UiValueUnit.Pixel(20), UiValueUnit.Pixel(100));
            x.HorizontalAnchor = new UiAnchor(UiAlignment.Right, UiValueUnit.Pixel(20), UiValueUnit.Pixel(100));
        }).Register(entityComponentSystem);*/

        spinner = new Rect().Configure<Rect, RenderMesh3D>(x =>
            {
                x.SetMesh(MeshPrefab.Rect);
                x.Render = true;
            })
            .Configure<Rect, UiTransform>(x =>
            {
                x.VerticalAnchor = new UiAnchor(UiAlignment.CenterVertical, 20, 10);
                x.HorizontalAnchor = new UiAnchor(UiAlignment.CenterHorizontal, 20, 10);
            })
            .Register(entityComponentSystem);

        //leftScreen.AddChild(rect.Get<UiTransform>());

        //static floor at -100 z

        //tile floor
        const int tileCount = 10;
        const int floorSize = 200;
        const int floorCenter = floorSize / 2;
        const int tileSize = floorSize / tileCount;

        for (int i = 0; i < tileCount; i++)
        {
            for (int j = 0; j < tileCount; j++)
            {
                var i1 = i;
                var j1 = j;
                var floor = new Cube(entityComponentSystem).Configure<Transform3d>(trans =>
                    {
                        trans.Position = new vec3(i1 * tileSize - floorCenter, -150, j1 * tileSize - floorCenter);
                        trans.Scale = new vec3(tileSize, 100, tileSize);
                    })
                    .Configure<ICollider>(x => { x.IsStatic = true; })
                    .Configure<PhysicsComponent>(x =>
                    {
                        x.IsStatic = true;
                        x.Force = vec3.Zero;
                        x.Velocity = vec3.Zero;
                    })
                    .Register(entityComponentSystem);
            }
        }

        for (int i = 0; i < 100; i++) //100 floors for more updates
        {
            var floor = new Cube(entityComponentSystem).Configure<Transform3d>(trans =>
                {
                    trans.Position = new vec3( tileSize - floorCenter, -150,  tileSize - floorCenter);
                    trans.Scale = new vec3(floorSize, 100, floorSize);
                })
                .Configure<ICollider>(x => { x.IsStatic = true; })
                .Configure<PhysicsComponent>(x =>
                {
                    x.IsStatic = true;
                    x.Force = vec3.Zero;
                    x.Velocity = vec3.Zero;
                })
                .Register(entityComponentSystem);
        }
        
        
        for (var i = 0; i < 10; i++)
        {
            //BUG: If we configure before register the data is not uploaded properly
            var cube = new Cube(entityComponentSystem)
                .Register(entityComponentSystem)
                .Configure<Transform3d>(trans =>
                {
                    trans.Position = new vec3(r.Next(-posRange, posRange), r.Next(-posRange, posRange), r.Next(-posRange, posRange));
                    trans.Rotation = new vec3(r.Next(0, 100), r.Next(0, 100), r.Next(0, 100));
                });
        }
    }

    private void WindowOnOnKeyEvent(object? sender, Key key, int scancode, InputAction inputaction, Modifier modifiers)
    {
        if (inputaction != InputAction.Press) return;
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        var meshPref = MeshPrefab.Cube;
        var wp = entityComponentSystem.Get<IWorkerPool>();

        if (key is > Key.Num0 and <= Key.Num9)
        {
            var index = key - Key.Num0 + 1;
            var rep = 1 << index;
            const float sz = .5f;
            wp.EnqueueWork((info, param) =>
            {
                using var change = entityComponentSystem.Get<IGraphicsSystem>().ChangingObserver.BeginBigChange();

                var res = Parallel.For(0, rep, new ParallelOptions
                    { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 }, i =>
                {
                    for (var j = 0; j < rep; j++)
                    {
                        var cube = new Cube(entityComponentSystem)
                            .Register(entityComponentSystem)
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
            }, ALog.Error, $"Creation of {rep * rep} Cubes");
        }

        switch (key)
        {
            case Key.Q:
                Task.Run(() => entityComponentSystem.Get<IGraphicsSystem>().UpdateGraphicsData());
                break;
            case Key.B:
                {
                    using var change = entityComponentSystem.Get<GraphicsSystem>().ChangingObserver.BeginBigChange();

                    for (var i = 0; i < 1000; i++)
                    {
                        var cube = new Cube(entityComponentSystem)
                            .Register(entityComponentSystem)
                            .Configure<Transform3d>(trans =>
                            {
                                trans.Position = new vec3(r.Next(-posRange, posRange), r.Next(-posRange, posRange), r.Next(-posRange, posRange));
                                trans.Rotation = new vec3(r.Next(0, 100), r.Next(0, 100), r.Next(0, 100));
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
                var bbs = entityComponentSystem.Get<BoundingBoxComponentsSystem>();
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
                var sys = entityComponentSystem.Get<Ajiva3dLayerSystem>();
                var cubex = new Cube(entityComponentSystem).Configure<Transform3d>(trans =>
                    {
                        trans.Position = sys.MainCamara.Transform.Position + sys.MainCamara.FrontNormalized * 25;
                        trans.Rotation = sys.MainCamara.Transform.Rotation;
                        trans.Scale = new vec3(3);
                    })
                    .Configure<ICollider>(x => { x.IsStatic = true; })
                    .Configure<PhysicsComponent>(x => { x.IsStatic = true; })
                    .Register(entityComponentSystem);

                break;
            case Key.G:
                var sys2 = entityComponentSystem.Get<Ajiva3dLayerSystem>();
                var cubex2 = new Cube(entityComponentSystem).Configure<Transform3d>(trans =>
                    {
                        trans.Position = sys2.MainCamara.Transform.Position;
                        trans.Scale = new vec3(10);
                    })
                    .Configure<ICollider>(x => { x.IsStatic = true; })
                    .Configure<PhysicsComponent>(x => { x.IsStatic = true; })
                    .Register(entityComponentSystem);

                break;
        }
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        entityComponentSystem.Get<DeviceSystem>().WaitIdle();

        entityComponentSystem.Dispose();

        debugReportCallback.Dispose();
        vulcanInstance.Dispose();
        debugReportCallback = null!;
        vulcanInstance = null!;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
