//#define TEST_MODE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ajiva.Engine;
using ajiva.EngineManagers;
using ajiva.Models;
using GlmSharp;
using SharpVk;
using SharpVk.Glfw;
using SharpVk.Khronos;
using SharpVk.Multivendor;
using Semaphore = SharpVk.Semaphore;

namespace ajiva
{
    public partial class Program : DisposingLogger
    {
        private readonly AjivaRenderEngine engine;
        private const int SurfaceWidth = 800;
        private const int SurfaceHeight = 600;

#pragma warning disable 1998
        private static async Task Main()
#pragma warning restore 1998
        {
            AjivaRenderEngine renderEngine = null!;
            Glfw3.Init();

            (Instance instance, DebugReportCallback debugReportCallback) = AjivaRenderEngine.CreateInstance(Glfw3.GetRequiredInstanceExtensions());
#if TEST_MODE
            for (var i = 0; i < 50; i++)
            {
                var i1 = i;
                var tr = new Thread(async () =>
                {
                    Thread.Sleep(i1 * 500);
                    var engine = new AjivaRenderEngine(instance);
                    var pg = new Program(engine);
                    await pg.Run(TimeSpan.FromMilliseconds(1000));
                    pg.Dispose();
                    Console.WriteLine("Finished: "+i1);
                });
                tr.Name = $"x-{i1}";
                tr.SetApartmentState(ApartmentState.STA);
                tr.Start();
            }

            await Task.Delay(-1);
#else
            renderEngine = new(instance);
            renderEngine.MainCamara = new Cameras.FpsCamera(90, SurfaceWidth, SurfaceHeight) {MovementSpeed = .1f};
            var meshPref = Mesh.Cube;
            var r = new Random();

            const int size = 30000;
            const int posRange = 100;
            const float scale = 0.5f;

            for (var i = 0; i < size; i++)
            {
                var verts = meshPref.VerticesData.ToArray();
                var inds = meshPref.IndicesData.ToArray();

                //var mesh = new Mesh(verts, inds);

                renderEngine.Entities.Add(new(new(
                        new(r.Next(-posRange, posRange), r.Next(-posRange, posRange), r.Next(-posRange, posRange)), new(r.Next(0, 100), r.Next(0, 100), r.Next(0, 100)),
                        new((float)(r.NextDouble() * scale))
                    ), meshPref
                ));
            }
            Console.WriteLine();

            var app = new Program(renderEngine);
            /*       
                   new Thread(() =>
                   {
                       while (true)
                       {
                           Thread.Sleep(100);
                           if (RenderEngine == null) continue;
                           if (!RenderEngine.Runing) continue;
                           if (app.applicationQueue.Count > 1000) continue;
                           app.applicationQueue.Enqueue(() =>
                           {
                               RenderEngine.RecreateSwapChain();
                           });
                       }
                   }).Start(); */

            await app.Run(TimeSpan.MaxValue);
#endif

            debugReportCallback.Dispose();
            instance.Dispose();

            Glfw3.Terminate();
            Console.WriteLine("Finished, press any Key to continue.");
            Console.ReadKey();
            Environment.Exit(0);
        }

        private async Task Run(TimeSpan maxValue)
        {
            await engine.InitWindow(SurfaceWidth, SurfaceHeight);
            InitEvents();
            await engine.InitVulkan();
            UpdateUniformBuffer(TimeSpan.Zero);
            await engine.MainLoop(maxValue);
            await engine.Cleanup();
        }

        private void InitEvents()
        {
            engine.OnUpdate += OnUpdate;
        }

        private void OnUpdate(object sender, TimeSpan delta)
        {
            UpdateApplication();

            UpdateUniformBuffer(delta);
        }

        private void OnFrame(object sender, TimeSpan delta)
        {
        }

        private Random r = new Random();

        private void UpdateUniformBuffer(TimeSpan timeSpan)
        {
            var currentTimestamp = Stopwatch.GetTimestamp();
            var totalTime = (currentTimestamp - engine.InitialTimestamp) / (float)Stopwatch.Frequency;

            var delta = (float)timeSpan.TotalMilliseconds;
            var translate = MathF.Sin(totalTime) / 100f;

            foreach (var aEntity in engine.Entities.Where(e => e.RenderAble != null && e.RenderAble.Render))
            {
                //aEntity.Transform.Rotation = new(r.Next(0, 100), r.Next(0, 100), r.Next(0, 100));
                aEntity.Transform.Position.x += translate;
                aEntity.Transform.Rotation.x += delta;
                if (engine.ShaderComponent.UniformModels.Staging.Value.Length > aEntity.RenderAble!.Id)
                {
                    engine.ShaderComponent.UniformModels.Staging.Value[aEntity.RenderAble!.Id] = new() {Model = aEntity.Transform.ModelMat};
                }
            }

            engine.ShaderComponent.UniformModels.Staging.CopyValueToBuffer();
            lock (engine.RenderLock)
                engine.ShaderComponent.UniformModels.Copy();
        }

        private readonly Queue<Action> applicationQueue = new();

        private Program(AjivaRenderEngine engine)
        {
            this.engine = engine;
        }

        private void UpdateApplication()
        {
            while (applicationQueue.TryDequeue(out var action))
                action.Invoke();
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
        }
    }
}
