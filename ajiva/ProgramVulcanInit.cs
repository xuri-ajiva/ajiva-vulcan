using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.CompilerServices;
using ajiva.Models;
using SharpVk;
using SharpVk.Khronos;
using SharpVk.Multivendor;
using Buffer = SharpVk.Buffer;
using Image = SharpVk.Image;

namespace ajiva
{
    public partial class Program
    {
        private void CreateInstance()
        {
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
                    ApplicationName = "vc-01",
                    ApplicationVersion = new(0, 1, 0),
                    EngineName = "xaphante",
                    EngineVersion = new(0, 0, 1),
                    ApiVersion = new(1, 0, 0)
                });

            Instance.CreateDebugReportCallback(DebugReportDelegate, DebugReportFlags.Error | DebugReportFlags.Warning | DebugReportFlags.Information | DebugReportFlags.PerformanceWarning);

            foreach (var extension in SharpVk.Instance.EnumerateExtensionProperties())
                Console.WriteLine($"Extension available: {extension.ExtensionName}");

            foreach (var layer in SharpVk.Instance.EnumerateLayerProperties())
                Console.WriteLine($"Layer available: {layer.LayerName}, {layer.Description}");
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
