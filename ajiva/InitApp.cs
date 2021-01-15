//#define TEST_MODE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ajiva.Engine;
using ajiva.EngineManagers;
using ajiva.Entity;
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
            AjivaRenderEngine RenderEngine = null;
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
            RenderEngine = new(instance);
            var meshPref = Mesh.Cube;
            var r = new Random();

            for (var i = 0; i < 100; i++)
            {
                var verts = meshPref.VerticesData.ToArray();
                var inds = meshPref.IndicesData.ToArray();

                var rx = r.Next(0, 100);
                var ry = r.Next(0, 100);
                var rz = r.Next(0, 100);

                for (int j = 0; j < verts.Length; j++)
                {
                    verts[j].Position.x += rx;
                    verts[j].Position.y += ry;
                    verts[j].Position.z += rz;
                }

                renderEngine.Entities.Add(new(Transform3d.Default, new Mesh(verts, inds)));
            }

            var app = new Program(RenderEngine);
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
            Environment.Exit(0);
        }

        private async Task Run(TimeSpan maxValue)
        {
            await InitWindow(engine);
            Thread.Sleep(200);
            engine.InitVulkan();
            Thread.Sleep(200);
            camera.MovementSpeed = .1f;
            camera.Transform.Position.z -= 1;
            Thread.Sleep(200);
            engine.MainLoop(maxValue);
            engine.Cleanup();
        }

        private readonly Cameras.FpsCamera camera = new(90, SurfaceWidth, SurfaceHeight);

        private async Task InitWindow(AjivaRenderEngine engine)
        {
            await engine.Window.InitWindow(SurfaceWidth, SurfaceHeight);
            engine.Window.OnFrame += OnFrame;
            engine.Window.OnResize += delegate

            {
                engine.RecreateSwapChain();
            };

            engine.Window.OnKeyEvent += delegate(object? _, Key key, int _, InputAction action, Modifier _)
            {
                var down = action != InputAction.Release;

                switch (key)
                {
                    case Key.W:
                        camera.keys.up = down;
                        break;
                    case Key.D:
                        camera.keys.right = down;
                        break;
                    case Key.S:
                        camera.keys.down = down;
                        break;
                    case Key.A:
                        camera.keys.left = down;
                        break;
                }
            };

            engine.Window.OnMouseMove += delegate(object? _, vec2 vec2)
            {
                camera.OnMouseMoved(vec2.x, vec2.y);
            };
        }

        private void OnFrame(object sender, TimeSpan delta)
        {
            camera.Update((float)delta.TotalMilliseconds);
            //Console.WriteLine(camera.Position);
            //Console.WriteLine(camera.Rotation);

            UpdateApplication();
            UpdateUniformBuffer();
            DrawFrame();
        }

        private void DrawFrame()
        {
            ATrace.Assert(engine.SwapChainComponent.SwapChain != null, "SwapChainComponent.SwapChain != null");
            var nextImage = engine.SwapChainComponent.SwapChain.AcquireNextImage(uint.MaxValue, engine.SemaphoreComponent.ImageAvailable, null);

            var si = new SubmitInfo
            {
                CommandBuffers = new[]
                {
                    engine.DeviceComponent.CommandBuffers[nextImage]
                },
                SignalSemaphores = new[]
                {
                    engine.SemaphoreComponent.RenderFinished
                },
                WaitDestinationStageMask = new[]
                {
                    PipelineStageFlags.ColorAttachmentOutput
                },
                WaitSemaphores = new[]
                {
                    engine.SemaphoreComponent.ImageAvailable
                }
            };
            engine.DeviceComponent.GraphicsQueue.Submit(si, null);
            var result = new Result[1];
            engine.DeviceComponent.PresentQueue.Present(engine.SemaphoreComponent.RenderFinished, engine.SwapChainComponent.SwapChain, nextImage, result);
            si.SignalSemaphores = Array.Empty<Semaphore>();
            si.WaitSemaphores = Array.Empty<Semaphore>();
            si.WaitDestinationStageMask = Array.Empty<PipelineStageFlags>();
            si.CommandBuffers = Array.Empty<CommandBuffer>();
            result = Array.Empty<Result>();
            si = new();
        }

        UniformBufferData ubo;

        private void UpdateUniformBuffer()
        {
            var currentTimestamp = Stopwatch.GetTimestamp();

            var totalTime = (currentTimestamp - engine.InitialTimestamp) / (float)Stopwatch.Frequency;

            ubo = new()
            {
                Model = mat4.Identity, //mat4.Rotate((float)Math.Sin(totalTime) * (float)Math.PI, vec3.UnitZ),
                View = camera.View, //mat4.LookAt(new(2), vec3.Zero, vec3.UnitZ),
                Proj = camera.Projection //mat4.Perspective((float)Math.PI / 4f, swapChainExtent.Width / (float)swapChainExtent.Height, 0.1f, 10)
            };

            ubo.Proj[1, 1] *= -1;

            var ubx = new UniformViewProj()
            {
                View = camera.View, //mat4.LookAt(new(2), vec3.Zero, vec3.UnitZ),
                Proj = camera.Projection //mat4.Perspective((float)Math.PI / 4f, swapChainExtent.Width / (float)swapChainExtent.Height, 0.1f, 10)
            };
            ubx.Proj[1, 1] *= -1;

            engine.ShaderComponent.UniformModels.Update(new[]
            {
                new UniformModel()
                {
                    Model = mat4.Identity //mat4.Rotate((float)Math.Sin(totalTime) * (float)Math.PI, vec3.UnitZ)
                }
            });
            engine.ShaderComponent.UniformModels.Copy();
            engine.ShaderComponent.ViewProj.Update(new[]
            {
                ubx
            });
            engine.ShaderComponent.ViewProj.Copy();

            /*uint uboSize = (uint)Unsafe.SizeOf<UniformBufferObject>();

            IntPtr memoryBuffer = AEntityComponent.UniformStagingBufferMemory.Map(0, uboSize, MemoryMapFlags.None);

            Marshal.StructureToPtr(ubo, memoryBuffer, false);

            AEntityComponent.UniformStagingBufferMemory.Unmap();

            DeviceComponent.CopyBuffer(AEntityComponent.UniformStagingBuffer, AEntityComponent.UniformBuffer, uboSize);*/
            //ShaderComponent.Uniform.Update(new []{ubo});
            //ShaderComponent.Uniform.Copy();
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
            camera.Dispose();
        }
    }
}
