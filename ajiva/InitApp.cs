using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ajiva.EngineManagers;
using ajiva.Models;
using GlmSharp;
using SharpVk;
using SharpVk.Glfw;
using SharpVk.Khronos;

namespace ajiva
{
    public partial class Program
    {
#pragma warning disable 1998
        private static async Task Main()
#pragma warning restore 1998
        {
            var pg = new Program();

            pg.Run();
            Environment.Exit(0);
        }

        private readonly Cameras.FpsCamera camera = new(90, SurfaceWidth, SurfaceHeight);

        private void Run()
        {
            InitWindow();
            InitVulkan();

            camera.MovementSpeed = .1f;
            camera.Transform.Position.z -= 1;
            initialTimestamp = Stopwatch.GetTimestamp();
            Window.MainLoop();
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
            var nextImage = SwapChainManager.SwapChain.AcquireNextImage(uint.MaxValue, SemaphoreManager.ImageAvailable, null);

            DeviceManager.GraphicsQueue.Submit(new SubmitInfo
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
            }, null);

            DeviceManager.PresentQueue.Present(SemaphoreManager.RenderFinished, SwapChainManager.SwapChain, nextImage, new Result[1]);
        }

        private void UpdateUniformBuffer()
        {
            long currentTimestamp = Stopwatch.GetTimestamp();

            var totalTime = (currentTimestamp - initialTimestamp) / (float)Stopwatch.Frequency;

            var ubo = new UniformBufferObject
            {
                Model = mat4.Rotate((float)Math.Sin(totalTime) * (float)Math.PI, vec3.UnitZ),
                View = camera.View, //mat4.LookAt(new(2), vec3.Zero, vec3.UnitZ),
                Proj = camera.Projection //mat4.Perspective((float)Math.PI / 4f, swapChainExtent.Width / (float)swapChainExtent.Height, 0.1f, 10)
            };

            ubo.Proj[1, 1] *= -1;

            uint uboSize = (uint)Unsafe.SizeOf<UniformBufferObject>();

            IntPtr memoryBuffer = BufferManager.UniformStagingBufferMemory.Map(0, uboSize, MemoryMapFlags.None);

            Marshal.StructureToPtr(ubo, memoryBuffer, false);

            BufferManager.UniformStagingBufferMemory.Unmap();

            DeviceManager.CopyBuffer(BufferManager.UniformStagingBuffer, BufferManager.UniformBuffer, uboSize);
        }

        private readonly Queue<Action> applicationQueue = new();

        private void UpdateApplication()
        {
            if (applicationQueue.TryDequeue(out var action))
                action.Invoke();
        }
    }
}
