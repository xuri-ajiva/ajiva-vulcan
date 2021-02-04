using System;
using ajiva.Components;
using ajiva.Ecs;
using ajiva.Ecs.Example;
using ajiva.Entities;
using ajiva.Factories;
using ajiva.Generators.Texture;
using ajiva.Helpers;
using ajiva.Models;
using ajiva.Systems;
using ajiva.Systems.VulcanEngine;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Systems.VulcanEngine.Ui;
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
            (vulcanInstance, debugReportCallback) = AjivaRenderEngine.CreateInstance(Glfw3.GetRequiredInstanceExtensions());
            var renderEngine = new AjivaRenderEngine();
            var deviceSystem = new DeviceSystem();
            var pool = new WorkerPool(Environment.ProcessorCount / 2, "AjivaWorkerPool");

            entityComponentSystem.AddInstance(vulcanInstance);

            entityComponentSystem.AddSystem(deviceSystem);
            entityComponentSystem.AddSystem(new ShaderSystem());
            entityComponentSystem.AddSystem(new WindowSystem());
            entityComponentSystem.AddSystem(new GraphicsSystem());
            entityComponentSystem.AddSystem(pool);
            entityComponentSystem.AddSystem(new BoxTextureGenerator());

            entityComponentSystem.AddComponentSystem(renderEngine);
            entityComponentSystem.AddComponentSystem(new UiRenderer());
            entityComponentSystem.AddComponentSystem(new TextureSystem());
            entityComponentSystem.AddComponentSystem(new ImageSystem());
            entityComponentSystem.AddComponentSystem(new TransformComponentSystem());

            entityComponentSystem.AddEntityFactory(typeof(SdtEntity), new SomeEntityFactory());

            entityComponentSystem.AddEntityFactory(typeof(Cube), new CubeFactory());
            entityComponentSystem.AddEntityFactory(typeof(Rect), new RectFactory());
            entityComponentSystem.AddEntityFactory(typeof(Cameras.FpsCamera), new Cameras.FpsCamaraFactory());

            entityComponentSystem.AddParam(nameof(SurfaceHeight), SurfaceHeight);
            entityComponentSystem.AddParam(nameof(SurfaceWidth), SurfaceWidth);

            renderEngine.MainCamara = entityComponentSystem.CreateEntity<Cameras.FpsCamera>();
            renderEngine.MainCamara.UpdatePerspective(90, SurfaceWidth, SurfaceHeight);
            renderEngine.MainCamara.MovementSpeed = .1f;

            var meshPref = MeshPrefab.Cube.Clone();
            var r = new Random();

            const int size = 10;
            const int posRange = 10;
            const float scale = 0.7f;

            entityComponentSystem.SetupSystems();
            entityComponentSystem.InitSystems();

            pool.Enabled = true;

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
        protected override void ReleaseUnmanagedResources()
        {
            entityComponentSystem.Dispose();

            debugReportCallback.Dispose();
            vulcanInstance.Dispose();
            debugReportCallback = null;
            vulcanInstance = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
