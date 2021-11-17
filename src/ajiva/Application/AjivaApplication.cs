using ajiva.Components.Physics;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Ecs.Example;
using ajiva.Entities;
using ajiva.Factories;
using ajiva.Generators.Texture;
using ajiva.Systems;
using ajiva.Systems.Assets;
using ajiva.Systems.Physics;
using ajiva.Systems.VulcanEngine;
using ajiva.Systems.VulcanEngine.Debug;
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
    private const int posRange = 10;
    private const float scale = 0.7f;

    private readonly IAjivaEcs entityComponentSystem;
    private DebugReportCallback debugReportCallback;
    private readonly Random r = new Random();

    private Instance vulcanInstance;

    public AjivaApplication(CancellationTokenSource tokenSource)
    {
        entityComponentSystem = new AjivaEcs(tokenSource);
    }

    private bool Running { get; set; }
    

    public async Task Run()
    {
        Running = true;
        await entityComponentSystem.RunUpdates();
        Running = false;
    }

    public void Init()
    {
        entityComponentSystem.AddParam(Const.Default.Config, Config.Default);

        (vulcanInstance, debugReportCallback) = Statics.CreateInstance(Glfw3.GetRequiredInstanceExtensions());
        var deviceSystem = entityComponentSystem.CreateSystemOrComponentSystem<DeviceSystem>();

        var meshPool = new MeshPool(deviceSystem);
        entityComponentSystem.AddInstance(meshPool);

        entityComponentSystem.AddInstance(vulcanInstance);

        entityComponentSystem.AddSystem(new WorkerPool(Environment.ProcessorCount / 2, "AjivaWorkerPool", entityComponentSystem) { Enabled = true });
        var window = entityComponentSystem.CreateSystemOrComponentSystem<WindowSystem>();
        entityComponentSystem.CreateSystemOrComponentSystem<BoxTextureGenerator>();

        //var layerSystem = entityComponentSystem.CreateSystemOrComponentSystem<LayerSystem>();

        entityComponentSystem.CreateSystemOrComponentSystem<TextureSystem>();
        entityComponentSystem.CreateSystemOrComponentSystem<ImageSystem>();
        entityComponentSystem.CreateSystemOrComponentSystem<TransformComponentSystem>();
        entityComponentSystem.CreateSystemOrComponentSystem<Transform2dComponentSystem>();
        entityComponentSystem.CreateSystemOrComponentSystem<AssetManager>();

        var graphicsSystem = entityComponentSystem.CreateSystemOrComponentSystem<GraphicsSystem>();
        entityComponentSystem.AddEntityFactory(new SomeEntityFactory());

        entityComponentSystem.AddEntityFactory(new CubeFactory(meshPool));
        entityComponentSystem.AddEntityFactory(new RectFactory());
        entityComponentSystem.AddEntityFactory(new Cameras.FpsCamaraFactory());
        entityComponentSystem.AddEntityFactory(new DebugBoxFactory());

        window.OnKeyEvent += WindowOnOnKeyEvent;
        var ajiva3dLayerSystem = entityComponentSystem.CreateSystemOrComponentSystem<Ajiva3dLayerSystem>();
        var ajiva2dLayerSystem = entityComponentSystem.CreateSystemOrComponentSystem<Ajiva2dLayerSystem>();
        var solidMeshRenderLayer = entityComponentSystem.CreateSystemOrComponentSystem<SolidMeshRenderLayer>();
        var debugLayer = entityComponentSystem.CreateSystemOrComponentSystem<DebugLayer>();
        var rectRender = entityComponentSystem.CreateSystemOrComponentSystem<Mesh2dRenderLayer>();

        var collisionsComponentSystem = entityComponentSystem.CreateSystemOrComponentSystem<CollisionsComponentSystem>();
        var boundingBoxComponentsSystem = entityComponentSystem.CreateSystemOrComponentSystem<BoundingBoxComponentsSystem>();

        graphicsSystem.AddUpdateLayer(ajiva3dLayerSystem);
        graphicsSystem.AddUpdateLayer(ajiva2dLayerSystem);

        ajiva3dLayerSystem.AddLayer(solidMeshRenderLayer);
        ajiva3dLayerSystem.AddLayer(debugLayer);
        ajiva2dLayerSystem.AddLayer(rectRender);

        var meshPref = MeshPrefab.Cube;
        var r = new Random();

        entityComponentSystem.InitSystems();

        meshPool.AddMesh(MeshPrefab.Cube);
        meshPool.AddMesh(MeshPrefab.Rect);

        if (entityComponentSystem.TryCreateEntity<Rect>(out var rect))
        {
            if (rect.TryGetComponent<RenderMesh2D>(out var rectMesh))
            {
                rectMesh.SetMesh(MeshPrefab.Rect);
                rectMesh.Render = true;
            }
            if (rect.TryGetComponent<Transform2d>(out var rectTrans)) rectTrans.Scale = new vec2(.05f);
        }

        for (var i = 0; i < 10; i++)
        {
            if (!entityComponentSystem.TryCreateEntity<Cube>(out var cube)) continue;
            if (cube.TryGetComponent<Transform3d>(out var trans))
            {
                trans.Position = new vec3(r.Next(-posRange, posRange), r.Next(-posRange, posRange), r.Next(-posRange, posRange));
                trans.Rotation = new vec3(r.Next(0, 100), r.Next(0, 100), r.Next(0, 100));
            }
        }
    }

    private void WindowOnOnKeyEvent(object? sender, Key key, int scancode, InputAction inputaction, Modifier modifiers)
    {
        if (inputaction != InputAction.Press) return;
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        var meshPref = MeshPrefab.Cube;
        var wp = entityComponentSystem.GetSystem<WorkerPool>();

        if (key is > Key.Num0 and <= Key.Num9)
        {
            var index = key - Key.Num0 + 1;
            var rep = 1 << index;
            const float sz = .5f;
            wp.EnqueueWork((info, param) =>
            {
                using var change = entityComponentSystem.GetSystem<GraphicsSystem>().ChangingObserver.BeginBigChange();

                var res = Parallel.For(0, rep, new ParallelOptions
                    { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 }, i =>
                {
                    for (var j = 0; j < rep; j++)
                    {
                        if (!entityComponentSystem.TryCreateEntity<Cube>(out var cube)) continue;

                        if (!cube.TryGetComponent<Transform3d>(out var trans)) continue;

                        trans.Position = new vec3(i * sz, -index * 2, j * sz);
                        trans.Rotation = new vec3(i * 90, j * 90, 0);
                        trans.Scale = new vec3(sz / 2);
                    }
                });
                while (!res.IsCompleted) Task.Delay(10);

                change.Dispose();
                return WorkResult.Succeeded;
            }, exception => ALog.Error(exception), $"Creation of {rep * rep} Cubes");
        }

        switch (key)
        {
            case Key.Q:
                Task.Run(() => entityComponentSystem.GetSystem<GraphicsSystem>().UpdateGraphicsData());
                break;
            case Key.F1:
                var s1 = entityComponentSystem.GetComponentSystem<DebugLayer, DebugComponent>();
                s1.Render.Value = !s1.Render;
                break;
            case Key.F2:
                var s2 = entityComponentSystem.GetComponentSystemUnSave<SolidMeshRenderLayer>();
                s2.Render.Value = !s2.Render;
                break;

            case Key.B:
                {
                    using var change = entityComponentSystem.GetSystem<GraphicsSystem>().ChangingObserver.BeginBigChange();

                    for (var i = 0; i < 100; i++)
                    {
                        if (!entityComponentSystem.TryCreateEntity<Cube>(out var cube)) continue;

                        if (cube.TryGetComponent<RenderMesh3D>(out var render))
                        {
                            render.SetMesh(meshPref);
                            render.Render = true;
                        }

                        if (cube.TryGetComponent<Transform3d>(out var trans))
                        {
                            trans.Position = new vec3(r.Next(-posRange, posRange), r.Next(-posRange, posRange), r.Next(-posRange, posRange));
                            trans.Rotation = new vec3(r.Next(0, 100), r.Next(0, 100), r.Next(0, 100));
                        }
                    }
                    change.Dispose();
                    break;
                }

            case Key.R:
                if (!entityComponentSystem.TryCreateEntity<Rect>(out var rect)) break;

                if (rect.TryGetComponent<RenderMesh2D>(out var renderRect))
                {
                    renderRect.SetMesh(MeshPrefab.Rect);
                    renderRect.Render = true;
                }
                break;

            case Key.P:
                var bbs = entityComponentSystem.GetComponentSystem<BoundingBoxComponentsSystem, BoundingBox>();
                wp.EnqueueWork((info, param) =>
                {
                    bbs.DoPhysicFrame();
                    return WorkResult.Succeeded;
                }, exception => ALog.Error(exception), "DoPhysicFrame");

                break;
            case Key.T:
                /*foreach (var keyValuePair in entityComponentSystem.Entities)
                {keyValuePair.Value
                }*/
                break;
            case Key.F:
                var sys = entityComponentSystem.GetSystem<Ajiva3dLayerSystem>();
                if (entityComponentSystem.TryCreateEntity<Cube>(out var cn))
                {
                    if (cn.TryGetComponent<RenderMesh3D>(out var render))
                    {
                        render.SetMesh(meshPref);
                        render.Render = true;
                    }

                    if (cn.TryGetComponent<Transform3d>(out var trans))
                    {
                        trans.Position = sys.MainCamara.Transform.Position + sys.MainCamara.FrontNormalized * 25;
                        trans.Rotation = sys.MainCamara.Transform.Rotation;
                    }
                }
                break;
        }
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        entityComponentSystem.GetSystem<DeviceSystem>().WaitIdle();

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
