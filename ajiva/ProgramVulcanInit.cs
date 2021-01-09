using System;
using System.Collections.Generic;
using System.Linq;
using SharpVk;
using SharpVk.Multivendor;

namespace ajiva
{
    public partial class Program
    {
        private void CreateInstance()
        {
            if (Instance != null) return;

            var enabledLayers = new List<string>();

            //VK_LAYER_LUNARG_api_dump
            //VK_LAYER_LUNARG_standard_validation

            void AddAvailableLayer(string layerName)
            {
                if (SharpVk.Instance.EnumerateLayerProperties().Any(x => x.LayerName == layerName))
                    enabledLayers.Add(layerName);
            }

            AddAvailableLayer("VK_LAYER_LUNARG_standard_validation");
            AddAvailableLayer("VK_LAYER_KHRONOS_validation");

            Instance = SharpVk.Instance.Create(
                enabledLayers.ToArray(),
                Window.GetRequiredInstanceExtensions().Append(ExtExtensions.DebugReport).ToArray(),
                applicationInfo: new ApplicationInfo
                {
                    ApplicationName = "ajiva",
                    ApplicationVersion = new(0, 0, 1),
                    EngineName = "ajiva-engine",
                    EngineVersion = new(0, 0, 1),
                    ApiVersion = new(1, 0, 0)
                });

            Instance.CreateDebugReportCallback(DebugReportDelegate, DebugReportFlags.Error | DebugReportFlags.Warning | DebugReportFlags.Information | DebugReportFlags.PerformanceWarning);

            //foreach (var extension in SharpVk.Instance.EnumerateExtensionProperties())
            //    Console.WriteLine($"Extension available: {extension.ExtensionName}");
            //
            //foreach (var layer in SharpVk.Instance.EnumerateLayerProperties())
            //    Console.WriteLine($"Layer available: {layer.LayerName}, {layer.Description}");
        }

        private void InitVulkan()
        {
            CreateInstance();
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
            BufferManager.CreateUniformBuffer();
            GraphicsManager.CreateDescriptorPool();
            GraphicsManager.CreateDescriptorSet();
            DeviceManager.CreateCommandBuffers();
            SemaphoreManager.CreateSemaphores();
        }
    }
}
