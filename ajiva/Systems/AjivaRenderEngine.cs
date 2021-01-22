using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ajiva.Components;
using ajiva.Ecs;
using ajiva.Ecs.Component;
using ajiva.Ecs.Entity;
using ajiva.Entitys;
using ajiva.Helpers;
using ajiva.Models;
using ajiva.Systems.RenderEngine.Engine;
using ajiva.Systems.RenderEngine.EngineManagers;
using GlmSharp;
using SharpVk;
using SharpVk.Khronos;
using SharpVk.Multivendor;

// ReSharper disable once CheckNamespace
namespace ajiva.Systems.RenderEngine
{
    public class AjivaRenderEngine : IRenderEngine, IDisposable
    {
        public AjivaRenderEngine(Instance instance)
        {
            Instance = instance;
            DeviceComponent = new(this);
            SwapChainComponent = new(this);
            ImageComponent = new(this);
            Window = new(this);
            GraphicsComponent = new(this);
            ShaderComponent = new(this);
            AEntityComponent = new(this);
            SemaphoreComponent = new(this);
            TextureComponent = new(this);
        }

        //public static Instance? Instance { get; set; }
        /// <inheritdoc />
        public bool Runing { get; set; }
        /// <inheritdoc />
        public Instance? Instance { get; set; }
        /// <inheritdoc />
        public DeviceComponent DeviceComponent { get; }
        /// <inheritdoc />
        public SwapChainComponent SwapChainComponent { get; }
        /// <inheritdoc />
        public PlatformWindow Window { get; }
        /// <inheritdoc />
        public ImageComponent ImageComponent { get; }
        /// <inheritdoc />
        public GraphicsComponent GraphicsComponent { get; }
        /// <inheritdoc />
        public ShaderComponent ShaderComponent { get; }
        /// <inheritdoc />
        public AEntityComponent AEntityComponent { get; }
        /// <inheritdoc />
        public SemaphoreComponent SemaphoreComponent { get; }
        /// <inheritdoc />
        public TextureComponent TextureComponent { get; }

        /// <inheritdoc />
        public event PlatformEventHandler OnFrame;
        /// <inheritdoc />
        public event PlatformEventHandler OnUpdate;
        /// <inheritdoc />
        public event KeyEventHandler OnKeyEvent;
        /// <inheritdoc />
        public event EventHandler OnResize;
        /// <inheritdoc />
        public event EventHandler<vec2> OnMouseMove;
        /// <inheritdoc />
        public Cameras.Camera MainCamara
        {
            get => mainCamara;
            set
            {
                MainCamara?.Dispose();
                mainCamara = value;
            }
        }
        /// <inheritdoc />
        public object RenderLock { get; } = new();
        /// <inheritdoc />
        public object UpdateLock { get; } = new();

        #region Public

        #region Vars

        private static readonly DebugReportCallbackDelegate DebugReportDelegate = DebugReport;

        public long InitialTimestamp;

        #endregion

        private static Bool32 DebugReport(DebugReportFlags flags, DebugReportObjectType objectType, ulong @object, HostSize location, int messageCode, string layerPrefix, string message, IntPtr userData)
        {
            Console.WriteLine($"[{flags}] ({objectType}) {layerPrefix}:\n{message}");

            return false;
        }

        public async Task Cleanup()
        {
            Dispose();
        }

        private void CleanupSwapChain()
        {
            ImageComponent.EnsureDepthResourcesDeletion();

            SwapChainComponent.EnsureSwapChainDeletion();

            DeviceComponent.EnsureCommandBuffersFree();

            GraphicsComponent.EnsureGraphicsLayoutDeletion();
        }

        public void RecreateSwapChain()
        {
            lock (UpdateLock)
            lock (RenderLock)
            {
                DeviceComponent.WaitIdle();
                CleanupSwapChain();

                MainCamara.UpdatePerspective(mainCamara.Fov, Window.Width, Window.Height);
                SwapChainComponent.EnsureSwapChainExists();
                ImageComponent.EnsureDepthResourcesExits();
                GraphicsComponent.EnsureGraphicsLayoutExists();
                GraphicsComponent.Current!.EnsureExists();
                SwapChainComponent.EnsureFrameBuffersExists();
                DeviceComponent.EnsureCommandBuffersExists();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!Runing) return;
            Runing = false;

            lock (UpdateLock)
            lock (RenderLock)
            {
                DeviceComponent.WaitIdle();

                SwapChainComponent.Dispose();
                ImageComponent.Dispose();
                GraphicsComponent.Dispose();
                ShaderComponent.Dispose();
                AEntityComponent.Dispose();
                SemaphoreComponent.Dispose();
                TextureComponent.Dispose();
                Window.Dispose();
                DeviceComponent.Dispose();
                MainCamara.Dispose();
                GC.SuppressFinalize(this);
                GC.Collect();
            }
        }

  #endregion

        public static (Instance instance, DebugReportCallback debugReportCallback) CreateInstance(IEnumerable<string> enabledExtensionNames)
        {
            //if (Instance != null) return;

            List<string> enabledLayers = new();

            var props = Instance.EnumerateLayerProperties();

            void AddAvailableLayer(string layerName)
            {
                if (props.Any(x => x.LayerName == layerName))
                    enabledLayers.Add(layerName);
            }

            AddAvailableLayer("VK_LAYER_LUNARG_standard_validation");
            AddAvailableLayer("VK_LAYER_KHRONOS_validation");
            AddAvailableLayer("VK_LAYER_GOOGLE_unique_objects");
            //AddAvailableLayer("VK_LAYER_LUNARG_api_dump");
            AddAvailableLayer("VK_LAYER_LUNARG_core_validation");
            AddAvailableLayer("VK_LAYER_LUNARG_image");
            AddAvailableLayer("VK_LAYER_LUNARG_object_tracker");
            AddAvailableLayer("VK_LAYER_LUNARG_parameter_validation");
            AddAvailableLayer("VK_LAYER_LUNARG_swapchain");
            AddAvailableLayer("VK_LAYER_GOOGLE_threading");

            var instance = Instance.Create(
                enabledLayers.ToArray(),
                enabledExtensionNames.Append(ExtExtensions.DebugReport).ToArray(),
                applicationInfo: new ApplicationInfo
                {
                    ApplicationName = "ajiva",
                    ApplicationVersion = new(0, 0, 1),
                    EngineName = "ajiva-engine",
                    EngineVersion = new(0, 0, 1),
                    ApiVersion = new(1, 0, 0)
                });

            var debugReportCallback = instance.CreateDebugReportCallback(DebugReportDelegate, DebugReportFlags.Error | DebugReportFlags.Warning | DebugReportFlags.PerformanceWarning);

            //foreach (var extension in SharpVk.Instance.EnumerateExtensionProperties())
            //    Console.WriteLine($"Extension available: {extension.ExtensionName}");
            //
            //foreach (var layer in SharpVk.Instance.EnumerateLayerProperties())
            //    Console.WriteLine($"Layer available: {layer.LayerName}, {layer.Description}");

            return (instance, debugReportCallback);
        }

        public async Task InitVulkan()
        {
            lock (RenderLock)
            {
                Window.EnsureSurfaceExists();
                DeviceComponent.EnsureDevicesExist();
                DeviceComponent.EnsureCommandPoolsExists();
                ShaderComponent.EnsureCreateUniformBufferExists();
                SwapChainComponent.EnsureSwapChainExists();
                GraphicsComponent.EnsureGraphicsLayoutExists();

                TextureComponent.EnsureDefaultImagesExists();
                GraphicsComponent.Current!.EnsureExists();
                ImageComponent.EnsureDepthResourcesExits();
                SwapChainComponent.EnsureFrameBuffersExists();
                foreach (var entity in Entities)
                {
                    entity.RenderAble?.Create(this);
                    AEntityComponent.Entities.Add(entity);
                }

                DeviceComponent.EnsureCommandBuffersExists();

                SemaphoreComponent.EnsureSemaphoresExists();
                GC.Collect();
            }
        }

        /// <inheritdoc />
        public List<AEntity> Entities { get; } = new();
        private Cameras.Camera mainCamara;

        public async Task MainLoop(TimeSpan maxValue)
        {
            InitialTimestamp = Stopwatch.GetTimestamp();
            Runing = true;

            Task[] tasks = {Task.Run(() => Window.RenderLoop(maxValue)), Task.Run(() => Window.UpdateLoop(maxValue))};
            Task.WaitAll(tasks);
        }

        public async Task InitWindow(int surfaceWidth, int surfaceHeight)
        {
            await Window.InitWindow(surfaceWidth, surfaceHeight);

            Window.OnResize += (_, eventArgs) =>
            {
                RecreateSwapChain();
                OnResize?.Invoke(this, eventArgs);
            };
            Window.OnUpdate += (_, delta) =>
            {
                OnUpdate?.Invoke(this, delta);
            };

            Window.OnFrame += (_, delta) =>
            {
                MainCamara.Update((float)delta.TotalMilliseconds);
                OnFrame?.Invoke(this, delta);
                UpdateCamaraProjView();
                DrawFrame();
            };
            Window.OnKeyEvent += delegate(object? _, Key key, int scancode, InputAction action, Modifier modifier)
            {
                var down = action != InputAction.Release;

                switch (key)
                {
                    case Key.W:
                        MainCamara.Keys.up = down;
                        break;
                    case Key.D:
                        MainCamara.Keys.right = down;
                        break;
                    case Key.S:
                        MainCamara.Keys.down = down;
                        break;
                    case Key.A:
                        MainCamara.Keys.left = down;
                        break;
                }

                OnKeyEvent?.Invoke(this, key, scancode, action, modifier);
            };

            Window.OnMouseMove += delegate(object? _, vec2 vec2)
            {
                MainCamara.OnMouseMoved(vec2.x, vec2.y);
                OnMouseMove?.Invoke(this, vec2);
            };
        }

        private void UpdateCamaraProjView()
        {
            ShaderComponent.ViewProj.UpdateExpresion(delegate(int index, ref UniformViewProj value)
            {
                if (index != 0) return;

                value.View = MainCamara.View;
                value.Proj = MainCamara.Projection;
                value.Proj[1, 1] *= -1;
            });
            ShaderComponent.ViewProj.Copy();
        }

        private void DrawFrame()
        {
            ATrace.Assert(SwapChainComponent.SwapChain != null, "SwapChainComponent.SwapChain != null");
            var nextImage = SwapChainComponent.SwapChain.AcquireNextImage(uint.MaxValue, SemaphoreComponent.ImageAvailable, null);

            var si = new SubmitInfo
            {
                CommandBuffers = new[]
                {
                    DeviceComponent.CommandBuffers![nextImage]
                },
                SignalSemaphores = new[]
                {
                    SemaphoreComponent.RenderFinished
                },
                WaitDestinationStageMask = new[]
                {
                    PipelineStageFlags.ColorAttachmentOutput
                },
                WaitSemaphores = new[]
                {
                    SemaphoreComponent.ImageAvailable
                }
            };
            DeviceComponent.GraphicsQueue!.Submit(si, null);
            var result = new Result[1];
            DeviceComponent.PresentQueue.Present(SemaphoreComponent.RenderFinished, SwapChainComponent.SwapChain, nextImage, result);
            si.SignalSemaphores = null!;
            si.WaitSemaphores = null!;
            si.WaitDestinationStageMask = null;
            si.CommandBuffers = null;
            result = null;
            si = default;
        }
    }
}
