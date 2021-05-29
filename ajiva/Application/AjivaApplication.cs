using System;
using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Ecs;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Example;
using ajiva.Entities;
using ajiva.Factories;
using ajiva.Generators.Texture;
using ajiva.Systems;
using ajiva.Systems.VulcanEngine;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Systems.VulcanEngine.Ui;
using ajiva.Utils;
using ajiva.Worker;
using SharpVk;
using SharpVk.Glfw;
using SharpVk.Multivendor;

namespace ajiva.Application
{
    public class AjivaApplication : DisposingLogger
    {
        private bool Running { get; set; }

        private readonly AjivaEcs entityComponentSystem = new(false);

        public void Run()
        {
            Running = true;

            RunHelper.RunDelta(delegate(UpdateInfo info)
            {
                entityComponentSystem.Update(info);
                return entityComponentSystem.Available;
            }, TimeSpan.MaxValue);

            Running = false;
        }

        private Instance vulcanInstance;
        private DebugReportCallback debugReportCallback;

        private const int SurfaceWidth = 800;
        private const int SurfaceHeight = 600;

        public void Init()
        {
            (vulcanInstance, debugReportCallback) = Statics.CreateInstance(Glfw3.GetRequiredInstanceExtensions());
            var deviceSystem = entityComponentSystem.CreateSystemOrComponentSystem<DeviceSystem>();

            var meshPool = new MeshPool(deviceSystem);
            entityComponentSystem.AddInstance(meshPool);

            entityComponentSystem.AddInstance(vulcanInstance);

            entityComponentSystem.AddSystem(new WorkerPool(Environment.ProcessorCount / 2, "AjivaWorkerPool", entityComponentSystem) {Enabled = true});
            entityComponentSystem.CreateSystemOrComponentSystem<ShaderSystem>();
            var window = entityComponentSystem.CreateSystemOrComponentSystem<WindowSystem>();
            entityComponentSystem.CreateSystemOrComponentSystem<BoxTextureGenerator>();

            var layerSystem = entityComponentSystem.CreateSystemOrComponentSystem<LayerSystem>();
            var renderEngine = entityComponentSystem.CreateSystemOrComponentSystem<Ajiva3dSystem>();
            var uiLayer = entityComponentSystem.CreateSystemOrComponentSystem<UiRenderer>();
            layerSystem.AddUpdateLayer(renderEngine);
            layerSystem.AddUpdateLayer(uiLayer);

            entityComponentSystem.CreateSystemOrComponentSystem<TextureSystem>();
            entityComponentSystem.CreateSystemOrComponentSystem<ImageSystem>();
            entityComponentSystem.CreateSystemOrComponentSystem<TransformComponentSystem>();

            entityComponentSystem.CreateSystemOrComponentSystem<GraphicsSystem>();
            entityComponentSystem.AddEntityFactory(new SomeEntityFactory());

            entityComponentSystem.AddEntityFactory(new CubeFactory());
            entityComponentSystem.AddEntityFactory(new RectFactory());
            entityComponentSystem.AddEntityFactory(new Cameras.FpsCamaraFactory());

            entityComponentSystem.AddParam(nameof(SurfaceHeight), SurfaceHeight);
            entityComponentSystem.AddParam(nameof(SurfaceWidth), SurfaceWidth);

            window.OnKeyEvent += WindowOnOnKeyEvent;

            renderEngine.MainCamara = entityComponentSystem.CreateEntity<Cameras.FpsCamera>();
            renderEngine.MainCamara.UpdatePerspective(90, SurfaceWidth, SurfaceHeight);
            renderEngine.MainCamara.MovementSpeed = .01f;

            var meshPref = MeshPrefab.Cube;
            var r = new Random();

            entityComponentSystem.InitSystems();

            meshPool.AddMesh(MeshPrefab.Cube);
            meshPool.AddMesh(MeshPrefab.Rect);

            for (var i = 0; i < 10; i++)
            {
                var cube = entityComponentSystem.CreateEntity<Cube>();

                var render = cube.GetComponent<RenderMesh3D>();
                render.SetMesh(meshPref);
                render.Render = true;

                var trans = cube.GetComponent<Transform3d>();
                trans.Position = new(r.Next(-posRange, posRange), r.Next(-posRange, posRange), r.Next(-posRange, posRange));
                trans.Rotation = new(r.Next(0, 100), r.Next(0, 100), r.Next(0, 100));
            }
        }

        const int size = 100;
        const int posRange = 100;
        const float scale = 0.7f;
        Random r = new();

        private void WindowOnOnKeyEvent(object? sender, Key key, int scancode, InputAction inputaction, Modifier modifiers)
        {
            if (inputaction != InputAction.Press) return;
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (key)
            {
                case Key.B:
                    for (var i = 0; i < 100; i++)
                    {
                        var meshPref = MeshPrefab.Cube;
                        var cube = entityComponentSystem.CreateEntity<Cube>();

                        var render = cube.GetComponent<RenderMesh3D>();
                        render.SetMesh(meshPref);
                        render.Render = true;

                        var trans = cube.GetComponent<Transform3d>();
                        trans.Position = new(r.Next(-posRange, posRange), r.Next(-posRange, posRange), r.Next(-posRange, posRange));
                        trans.Rotation = new(r.Next(0, 100), r.Next(0, 100), r.Next(0, 100));
                    }
                    break;

                case Key.R:
                    var rect = entityComponentSystem.CreateEntity<Rect>();

                    var renderRect = rect.GetComponent<RenderMesh2D>();
                    renderRect.SetMesh(MeshPrefab.Rect);
                    renderRect.Render = true;
                    break;

                case Key.T:
                    /*foreach (var keyValuePair in entityComponentSystem.Entities)
                    {keyValuePair.Value
                    }*/
                    break;
            }
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
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
}
