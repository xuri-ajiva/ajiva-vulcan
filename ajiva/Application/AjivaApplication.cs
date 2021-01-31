using System;
using ajiva.Components;
using ajiva.Ecs;
using ajiva.Ecs.Example;
using ajiva.Entities;
using ajiva.Factories;
using ajiva.Helpers;
using ajiva.Models;
using ajiva.Systems;
using ajiva.Systems.VulcanEngine;
using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;
using SharpVk.Glfw;
using SharpVk.Multivendor;

namespace ajiva.Application
{
    public class AjivaApplication : DisposingLogger
    {
        private bool Running { get; set; } = false;

        private readonly AjivaEcs entityComponentSystem = new(true);

        public void Run()
        {
            Running = true;

            Helpers.RunHelper.RunDelta(delegate(TimeSpan span)
            {
                entityComponentSystem.Update(span);
                if (!entityComponentSystem.Available)
                    Running = false;
            }, () => Running, TimeSpan.MaxValue);
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

            entityComponentSystem.AddInstance(vulcanInstance);

            entityComponentSystem.AddSystem(deviceSystem);
            entityComponentSystem.AddSystem(new ShaderSystem(deviceSystem));
            entityComponentSystem.AddSystem(new WindowSystem());
            entityComponentSystem.AddSystem(new GraphicsSystem());

            entityComponentSystem.AddComponentSystem(renderEngine);
            entityComponentSystem.AddComponentSystem(new TextureSystem());
            entityComponentSystem.AddComponentSystem(new ImageSystem());
            entityComponentSystem.AddComponentSystem(new TransformComponentSystem());

            entityComponentSystem.AddEntityFactory(typeof(SdtEntity), new SomeEntityFactory());

            entityComponentSystem.AddEntityFactory(typeof(Cube), new CubeFactory());
            entityComponentSystem.AddEntityFactory(typeof(Cameras.FpsCamera), new Cameras.FpsCamaraFactory());

            entityComponentSystem.AddParam(nameof(SurfaceHeight), SurfaceHeight);
            entityComponentSystem.AddParam(nameof(SurfaceWidth), SurfaceWidth);

            renderEngine.MainCamara = entityComponentSystem.CreateEntity<Cameras.FpsCamera>();
            renderEngine.MainCamara.UpdatePerspective(90, SurfaceWidth, SurfaceHeight);
            renderEngine.MainCamara.MovementSpeed = .1f;

            var meshPref = Mesh.Cube.Clone();
            var r = new Random();

            const int size = 10;
            const int posRange = 10;
            const float scale = 0.7f;

            entityComponentSystem.SetupSystems();
            entityComponentSystem.InitSystems();

            for (var i = 0; i < size; i++)
            {
                var cube = entityComponentSystem.CreateEntity<Cube>();

                var render = cube.GetComponent<ARenderAble>();
                render.SetMesh(meshPref, deviceSystem);
                render.Render = true;

                var trans = cube.GetComponent<Transform3d>();
                trans.Position = new(r.Next(-posRange, posRange), r.Next(-posRange, posRange), r.Next(-posRange, posRange));
                trans.Rotation = new(r.Next(0, 100), r.Next(0, 100), r.Next(0, 100));
            }
            Console.WriteLine();
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
