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

            entityComponentSystem.AddInstance(vulcanInstance);

            entityComponentSystem.AddSystem(new WorkerPool(Environment.ProcessorCount / 2, "AjivaWorkerPool", entityComponentSystem) {Enabled = true});
            entityComponentSystem.CreateSystemOrComponentSystem<ShaderSystem>();
            var window = entityComponentSystem.CreateSystemOrComponentSystem<WindowSystem>();
            entityComponentSystem.CreateSystemOrComponentSystem<GraphicsSystem>();
            entityComponentSystem.CreateSystemOrComponentSystem<BoxTextureGenerator>();

            var renderEngine = entityComponentSystem.CreateSystemOrComponentSystem<Ajiva3dSystem>();
            entityComponentSystem.CreateSystemOrComponentSystem<UiRenderer>();
            entityComponentSystem.CreateSystemOrComponentSystem<TextureSystem>();
            entityComponentSystem.CreateSystemOrComponentSystem<ImageSystem>();
            entityComponentSystem.CreateSystemOrComponentSystem<TransformComponentSystem>();

            entityComponentSystem.AddEntityFactory(new SomeEntityFactory());

            entityComponentSystem.AddEntityFactory(new CubeFactory());
            entityComponentSystem.AddEntityFactory(new RectFactory());
            entityComponentSystem.AddEntityFactory(new Cameras.FpsCamaraFactory());

            entityComponentSystem.AddParam(nameof(SurfaceHeight), SurfaceHeight);
            entityComponentSystem.AddParam(nameof(SurfaceWidth), SurfaceWidth);

            renderEngine.MainCamara = entityComponentSystem.CreateEntity<Cameras.FpsCamera>();
            renderEngine.MainCamara.UpdatePerspective(90, SurfaceWidth, SurfaceHeight);
            renderEngine.MainCamara.MovementSpeed = .01f;

            var meshPref = MeshPrefab.Cube.Clone();
            var r = new Random();

            const int size = 10;
            const int posRange = 10;
            const float scale = 0.7f;

            entityComponentSystem.InitSystems();

            for (var i = 0; i < size; i++)
            {
                var cube = entityComponentSystem.CreateEntity<Cube>();

                var render = cube.GetComponent<ARenderAble3D>();
                render.SetMesh(meshPref, deviceSystem);
                render.Render = true;

                var trans = cube.GetComponent<Transform3d>();
                trans.Position = new(r.Next(-posRange, posRange), r.Next(-posRange, posRange), r.Next(-posRange, posRange));
                trans.Rotation = new(r.Next(0, 100), r.Next(0, 100), r.Next(0, 100));
            }

            var rect = entityComponentSystem.CreateEntity<Rect>();

            var renderRect = rect.GetComponent<ARenderAble2D>();
            renderRect.SetMesh(MeshPrefab.Rect, deviceSystem);
            renderRect.Render = true;
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
