using System.Collections.Generic;
using System.Linq;
using SharpVk;
using SharpVk.Multivendor;

namespace ajiva
{
    public partial class Program
    {
        private static (Instance instance, DebugReportCallback debugReportCallback) CreateInstance(IEnumerable<string> enabledExtensionNames)
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

            return (instance,debugReportCallback);
        }

        private void InitVulkan()
        {
            Window.CreateSurface();
            DeviceManager.CreateDevice();
            SwapChainManager.CreateSwapChain();
            SwapChainManager.CreateImageViews();
            GraphicsManager.CreateRenderPass();
            GraphicsManager.CreateDescriptorSetLayout();
            ShaderManager.CreateShaderModules();
            GraphicsManager.CreateGraphicsPipeline();
            DeviceManager.CreateCommandPools();
            ImageManager.CreateDepthResources();
            SwapChainManager.CreateFrameBuffers();
            TextureManager.CreateLogo();
            BufferManager.AddBuffer(vertices, indices);
            ShaderManager.CreateUniformBuffer();
            GraphicsManager.CreateDescriptorPool();
            GraphicsManager.CreateDescriptorSet();
            DeviceManager.CreateCommandBuffers();
            SemaphoreManager.CreateSemaphores();
        }
    }
}
