using System.Numerics;
using ajiva.Components.Mesh;
using ajiva.Components.Physics;
using ajiva.Components.Transform;
using ajiva.Components.Transform.Ui;
using ajiva.Ecs;
using ajiva.Entities.Ui;
using ajiva.Extensions;
using ajiva.Systems.Physics;
using ajiva.Systems.VulcanEngine.Interfaces;
using ajiva.Systems.VulcanEngine.Layer3d;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;
using ajiva.Worker;
using Ajiva.Wrapper.Logger;
using Autofac;
using SharpVk.Glfw;

namespace ajiva.application;

public class Application : DisposingLogger
{
    private IContainer container;

    private ContainerProxy proxy;

    public Application(IContainer container, ContainerProxy proxy)
    {
        this.container = container;
        this.proxy = proxy;
        _factory = this.container.Resolve<EntityFactory>();
    }

    public void InitData()
    {
        var meshPref = MeshPrefab.Cube;

        var leftScreen = new UiTransform(null,
            new UiAnchor(UiAlignment.CenterVertical, 0, 1.0f),
            new UiAnchor(UiAlignment.Left, .1f, 0.4f)
        );
        //TODO proxy.RegisterComponent(AEntity.None, typeof(UiTransform), leftScreen);

/*var rect = new Rect().Configure<Rect, RenderMesh3D>(x =>
{
    x.SetMesh(MeshPrefab.Rect);
    x.Render = true;
}).Configure<Rect, UiTransform>(x =>
{
    x.VerticalAnchor = new UiAnchor(UiAlignment.CenterVertical, UiValueUnit.Pixel(20), UiValueUnit.Pixel(100));
    x.HorizontalAnchor = new UiAnchor(UiAlignment.Right, UiValueUnit.Pixel(20), UiValueUnit.Pixel(100));
}).Register(entityComponentSystem);*/

        spinner = container.CreateRect()
            .With(new UiTransform(null,
                UiAnchor.Pixel(10, 10, UiAlignment.Top),
                UiAnchor.Pixel(10, 10, UiAlignment.Left)
            ))
            .Finalize();

        /*spinner = new Rect().Configure<RenderInstanceMesh2D>(x =>
            {
                //mesh set in ctor
                /*x.SetMesh(MeshPrefab.Rect);
            x.Render = true;#1#
            })
            .Configure<UiTransform>(x =>
            {
                x.VerticalAnchor = new UiAnchor(UiAlignment.CenterVertical, 20, 10);
                x.HorizontalAnchor = new UiAnchor(UiAlignment.CenterHorizontal, 20, 10);
            })
            .Register(container);*/

//leftScreen.AddChild(rect.Get<UiTransform>());
        const int posRange = 20;

        for (var i = 0; i < 10; i++)
        {
            var cube = _factory.CreateCube()
                .With(new Transform3d() {
                    Position = new Vector3(Random.Shared.Next(-posRange, posRange), Random.Shared.Next(-posRange, posRange), Random.Shared.Next(-posRange, posRange)),
                    Rotation = new Vector3(Random.Shared.Next(0, 100), Random.Shared.Next(0, 100), Random.Shared.Next(0, 100)),
                })
                .Finalize()
                .Configure<CollisionsComponent>(x => { x.MeshId = meshPref.MeshId; });
        }
    }

    public void SetupUpdate()
    {
        container.Resolve<IWindowSystem>().OnKeyEvent += WindowOnOnKeyEvent;
        container.Resolve<IUpdateManager>().RegisterUpdateForAllInContainer();
    }

    public async Task Run(CancellationToken cancellation)
    {
        var updateManager = container.Resolve<IUpdateManager>();
        updateManager.Run();
        await updateManager.Wait(cancellation);
    }

    private Rect spinner;
    private readonly EntityFactory _factory;

    void WindowOnOnKeyEvent(object? sender, Key key, int scancode, InputAction inputaction, Modifier modifiers)
    {
        const int posRange = 100;
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
                        var cube = _factory.CreateCube()
                            .With(new Transform3d() {
                                //trans.Position = new vec3(i * sz, (-index * 2) * Math.Min(rep / (float)i, 10) , j * sz);
                                Position = new Vector3(i * sz, (-index * 2), j * sz),
                                Rotation = new Vector3(i * 90, j * 90, 0),
                                Scale = new Vector3(sz / 2.1f),
                            }).Finalize();
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
                        var cube = _factory.CreateCube()
                            .With(new Transform3d() {
                                Position = new Vector3(Random.Shared.Next(-posRange, posRange), Random.Shared.Next(-posRange, posRange), Random.Shared.Next(-posRange, posRange)),
                                Rotation = new Vector3(Random.Shared.Next(0, 100), Random.Shared.Next(0, 100), Random.Shared.Next(0, 100)),
                            }).Finalize();
                    }
                    change.Dispose();
                    break;
                }

            case Key.R:
                /*var rect = new Rect().Configure<UiTransform>(x =>
                {
                    x.Scale = new vec2(.05f);
                }).Register(entityComponentSystem);*/
                spinner.Get<UiTransform>().Rotation += new Vector2(0, .1f);
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
                var cubex = _factory.CreateCube()
                    .With(new Transform3d() {
                        Position = sys.MainCamara.Transform3d.Position + sys.MainCamara.FrontNormalized * 25,
                        Rotation = sys.MainCamara.Transform3d.Rotation,
                        Scale = new Vector3(3),
                    })
                    .Finalize()
                    .Configure<ICollider>(x => { x.IsStatic = true; })
                    .Configure<PhysicsComponent>(x => { x.IsStatic = true; });

                break;
            case Key.G:
                var sys2 = container.Resolve<Ajiva3dLayerSystem>();
                var cubex2 = _factory.CreateCube()
                    .With(new Transform3d() {
                        Position = sys2.MainCamara.Transform3d.Position,
                        Scale = new Vector3(10),
                    })
                    .Finalize()
                    .Configure<ICollider>(x => { x.IsStatic = true; })
                    .Configure<PhysicsComponent>(x => { x.IsStatic = true; });

                break;
        }
    }

    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        container.Resolve<IDeviceSystem>().WaitIdle();

        // container disposes all
        //container.Resolve<DebugReportCallback>().Dispose();
        //container.Resolve<Instance>().Dispose();

        //container.Dispose();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
