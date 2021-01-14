using System;
using ajiva.Engine;
using ajiva.EngineManagers;
using SharpVk;

namespace ajiva
{
    public partial class Program : IEngine, IDisposable
    {
        public Program(Instance instance)
        {
            Instance = instance;
            DeviceManager = new(this);
            SwapChainManager = new(this);
            ImageManager = new(this);
            Window = new PlatformWindow(this);
            GraphicsManager = new(this);
            ShaderManager = new(this);
            BufferManager = new(this);
            SemaphoreManager = new(this);
            TextureManager = new(this);
        }

        //public static Instance? Instance { get; set; }
        /// <inheritdoc />
        public bool Runing { get; set; }
        /// <inheritdoc />
        public Instance? Instance { get; set; }
        /// <inheritdoc />
        public DeviceManager DeviceManager { get; }
        /// <inheritdoc />
        public SwapChainManager SwapChainManager { get; }
        /// <inheritdoc />
        public IPlatformWindow Window { get; }
        /// <inheritdoc />
        public ImageManager ImageManager { get; }
        /// <inheritdoc />
        public GraphicsManager GraphicsManager { get; }
        /// <inheritdoc />
        public ShaderManager ShaderManager { get; }
        /// <inheritdoc />
        public BufferManager BufferManager { get; }
        /// <inheritdoc />
        public SemaphoreManager SemaphoreManager { get; }
        /// <inheritdoc />
        public TextureComponent TextureComponent { get; }
        
       
        /// <inheritdoc />
        public object Lock { get; } = new();

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

        public void Cleanup()
        {
            Dispose();
        }

        private void CleanupSwapChain()
        {
            ImageComponent.DepthImage.Dispose();

            SwapChainComponent.CleanupSwapChain();

            DeviceComponent.FreeCommandBuffers();

            GraphicsComponent.Pipeline.Dispose();

            GraphicsComponent.PipelineLayout.Dispose();

            GraphicsComponent.RenderPass.Dispose();

            GraphicsComponent.DescriptorPool.Dispose();
        }

        public void RecreateSwapChain()
        {
            lock (Lock)
            {
                DeviceComponent.WaitIdle();
                CleanupSwapChain();

                SwapChainComponent.CreateSwapChain();
                SwapChainComponent.CreateImageViews();
                ImageComponent.CreateDepthResources();
                GraphicsComponent.CreateRenderPass();
                SwapChainComponent.CreateFrameBuffers();
                //bufferManager.CreateUniformBuffer();   free in cleanup swapchain if created here
                GraphicsComponent.CreateGraphicsPipeline();
                GraphicsComponent.CreateDescriptorPool();
                GraphicsComponent.CreateDescriptorSet();
                DeviceComponent.CreateCommandBuffers();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (!Runing) return;
            Runing = false;

            DeviceComponent.WaitIdle();

            SwapChainComponent.Dispose();
            ImageComponent.Dispose();
            GraphicsComponent.Dispose();
            ShaderComponent.Dispose();
            AEnittyComponent.Dispose();
            SemaphoreComponent.Dispose();
            TextureComponent.Dispose();
            Window.Dispose();
            DeviceComponent.Dispose();
            Runing = false;
            GC.Collect();
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

        public void InitVulkan()
        {
            lock (Lock)
            {
                Window.CreateSurface();
                DeviceComponent.CreateDevice();
                SwapChainComponent.CreateSwapChain();
                SwapChainComponent.CreateImageViews();
                GraphicsComponent.CreateRenderPass();
                GraphicsComponent.CreateDescriptorSetLayout();
                ShaderComponent.CreateShaderModules();
                GraphicsComponent.CreateGraphicsPipeline();
                DeviceComponent.CreateCommandPools();
                ImageComponent.CreateDepthResources();
                SwapChainComponent.CreateFrameBuffers();
                TextureComponent.CreateLogo();
                foreach (var entity in Entities)
                {
                    entity.RenewAble.Create(this);
                    AEnittyComponent.Entities.Add(entity);
                }
                ShaderComponent.CreateUniformBuffer();
                GraphicsComponent.CreateDescriptorPool();
                GraphicsComponent.CreateDescriptorSet();
                DeviceComponent.CreateCommandBuffers();
                SemaphoreComponent.CreateSemaphores();
            }
        }

        public List<AEntity> Entities = new();

        public void MainLoop(TimeSpan maxValue)
        {
            InitialTimestamp = Stopwatch.GetTimestamp();
            Runing = true;

            Window.MainLoop(maxValue);
        }
    }
}
