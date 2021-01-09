//#define TEST_MODE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
    public partial class Program
    {
#pragma warning disable 1998
        private static async Task Main()
#pragma warning restore 1998
        {
            Glfw3.Init();
            ATrace.Log.Add(typeof(ABuffer));

            (Instance instance, DebugReportCallback debugReportCallback) = CreateInstance(Glfw3.GetRequiredInstanceExtensions());
#if TEST_MODE
            for (var i = 0; i < 50; i++)
            {
                var i1 = i;
                var tr = new Thread(() =>
                {
                    Thread.Sleep(i1 * 500);
                    var pg = new Program(instance);
                    pg.Run(TimeSpan.FromMilliseconds(1000));
                });
                tr.SetApartmentState(ApartmentState.STA);
                tr.Start();
            }

            await Task.Delay(-1);
#else
            var pg = new Program(instance);
            pg.Run(TimeSpan.MaxValue);
#endif

            debugReportCallback.Dispose();
            instance.Dispose();

            Glfw3.Terminate();
            Environment.Exit(0);
        }

        private readonly Cameras.FpsCamera camera = new(90, SurfaceWidth, SurfaceHeight);

        private void Run(TimeSpan timeToRun)
        {
            InitWindow();
            Thread.Sleep(200);
            InitVulkan();
            Thread.Sleep(200);
            camera.MovementSpeed = .1f;
            camera.Transform.Position.z -= 1;
            initialTimestamp = Stopwatch.GetTimestamp();
            Runing = true;
            Thread.Sleep(200);
            Window.MainLoop(timeToRun);
            Cleanup();
        }

        private void InitWindow()
        {
            Window.InitWindow(SurfaceWidth, SurfaceHeight);
            Window.OnFrame += OnFrame;
            Window.OnResize += delegate

            {
                RecreateSwapChain();
            };

            Window.OnKeyEvent += delegate(object? _, Key key, int _, InputAction action, Modifier _)
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

            Window.OnMouseMove += delegate(object? _, vec2 vec2)
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
            ATrace.Assert(SwapChainManager.SwapChain != null, "SwapChainManager.SwapChain != null");
            var nextImage = SwapChainManager.SwapChain.AcquireNextImage(uint.MaxValue, SemaphoreManager.ImageAvailable, null);

            SubmitInfo si = new SubmitInfo
            {
                CommandBuffers = new[]
                {
                    DeviceManager.CommandBuffers[nextImage]
                },
                SignalSemaphores = new[]
                {
                    SemaphoreManager.RenderFinished
                },
                WaitDestinationStageMask = new[]
                {
                    PipelineStageFlags.ColorAttachmentOutput
                },
                WaitSemaphores = new[]
                {
                    SemaphoreManager.ImageAvailable
                }
            };
            DeviceManager.GraphicsQueue.Submit(si, null);
            var result = new Result[1];
            DeviceManager.PresentQueue.Present(SemaphoreManager.RenderFinished, SwapChainManager.SwapChain, nextImage, result);
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

            var totalTime = (currentTimestamp - initialTimestamp) / (float)Stopwatch.Frequency;

            ubo = new()
            {
                Model = mat4.Rotate((float)Math.Sin(totalTime) * (float)Math.PI, vec3.UnitZ),
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

            ShaderManager.UniformModels.Update(new[]
            {
                new UniformModel()
                {
                    Model = mat4.Rotate((float)Math.Sin(totalTime) * (float)Math.PI, vec3.UnitZ)
                }
            });
            ShaderManager.UniformModels.Copy();
            ShaderManager.ViewProj.Update(new[]
            {
                ubx
            });
            ShaderManager.ViewProj.Copy();

            /*uint uboSize = (uint)Unsafe.SizeOf<UniformBufferObject>();

            IntPtr memoryBuffer = BufferManager.UniformStagingBufferMemory.Map(0, uboSize, MemoryMapFlags.None);

            Marshal.StructureToPtr(ubo, memoryBuffer, false);

            BufferManager.UniformStagingBufferMemory.Unmap();

            DeviceManager.CopyBuffer(BufferManager.UniformStagingBuffer, BufferManager.UniformBuffer, uboSize);*/
            //ShaderManager.Uniform.Update(new []{ubo});
            //ShaderManager.Uniform.Copy();
        }

        private readonly Queue<Action> applicationQueue = new();

        private void UpdateApplication()
        {
            if (applicationQueue.TryDequeue(out var action))
                action.Invoke();
        }
    }
}
