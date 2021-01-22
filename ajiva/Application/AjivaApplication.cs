using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ajiva.Components;
using ajiva.Ecs;
using ajiva.Ecs.Example;
using ajiva.Entitys;
using ajiva.Factorys;
using ajiva.Helpers;
using ajiva.Models;
using ajiva.Systems;
using ajiva.Systems.RenderEngine;
using SharpVk;
using SharpVk.Glfw;
using SharpVk.Multivendor;

namespace ajiva.Application
{
    public class AjivaApplication : DisposingLogger
    {
        public bool Runing { get; set; } = false;

        private AjivaEcs EntityComponentSystem = new();

        public async Task Run()
        {
            Runing = true;
            await RunDelta(delegate(TimeSpan span)
            {
                EntityComponentSystem.Update(span);
                if (!EntityComponentSystem.Available)
                    Runing = false;
            }, () => Runing, TimeSpan.MaxValue);
        }

        private static async Task RunDelta(Action<TimeSpan> action, Func<bool> condition, TimeSpan maxToRun)
        {
            var iteration = 0u;
            var start = DateTime.Now;

            var delta = TimeSpan.Zero;
            var now = Stopwatch.GetTimestamp();
            while (condition())
            {
                await Task.Delay(5);

                action?.Invoke(delta);

                iteration++;

                if (iteration % 10 == 0)
                {
                    if (DateTime.Now - start > maxToRun)
                    {
                        return;
                    }
                }
                var end = Stopwatch.GetTimestamp();
                delta = new(end - now);

                now = end;
            }
        }

        private Instance vulcanInstance;
        private DebugReportCallback debugReportCallback;

        private const int SurfaceWidth = 800;
        private const int SurfaceHeight = 600;

        public async Task Init()
        {
            (vulcanInstance, debugReportCallback) = AjivaRenderEngine.CreateInstance(Glfw3.GetRequiredInstanceExtensions());
            var renderEngine = new AjivaRenderEngine(vulcanInstance);
            EntityComponentSystem.AddComponentSystem(renderEngine);
            EntityComponentSystem.AddComponentSystem(new TransformComponentSystem());
            EntityComponentSystem.AddEntityFactory(typeof(SdtEntity), new SomeEntityFactory());
            EntityComponentSystem.AddEntityFactory(typeof(Cube), new CubeFactory());
            EntityComponentSystem.AddEntityFactory(typeof(Cameras.FpsCamera), new Cameras.FpsCamaraFactory());
            EntityComponentSystem.AddParam(nameof(SurfaceHeight), SurfaceHeight);
            EntityComponentSystem.AddParam(nameof(SurfaceWidth), SurfaceWidth);

            renderEngine.MainCamara = EntityComponentSystem.CreateEntity<Cameras.FpsCamera>();
            renderEngine.MainCamara.UpdatePerspective(90, SurfaceWidth, SurfaceHeight);
            renderEngine.MainCamara.MovementSpeed = .1f;

            var meshPref = Mesh.Cube;
            var r = new Random();

            const int size = 10;
            const int posRange = 10;
            const float scale = 0.7f;

            await EntityComponentSystem.InitSystems();

            for (var i = 0; i < size; i++)
            {
                var cube = EntityComponentSystem.CreateEntity<Cube>();

                var render = cube.GetComponent<ARenderAble>();
                render.SetMesh(meshPref, renderEngine.DeviceComponent);
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
            vulcanInstance.Dispose();
            debugReportCallback.Dispose();
            
            EntityComponentSystem.Dispose();
        }
    }
}
