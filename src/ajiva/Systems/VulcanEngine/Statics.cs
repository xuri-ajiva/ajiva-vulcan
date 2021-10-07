using System;
using System.Collections.Generic;
using System.Linq;
using ajiva.Components.Media;
using ajiva.Models;
using ajiva.Systems.VulcanEngine.Systems;
using Ajiva.Wrapper.Logger;
using SharpVk;
using SharpVk.Multivendor;

namespace ajiva.Systems.VulcanEngine
{
    public static class Statics
    {
        public static ImageView CreateImageView(this Image image, Device device, Format format, ImageAspectFlags aspectFlags)
        {
            return device.CreateImageView(image, ImageViewType.ImageView2d, format, ComponentMapping.Identity, new()
            {
                AspectMask = aspectFlags,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1,
            });
        }

        public static Format FindDepthFormat(this PhysicalDevice physicalDevice)
        {
            return FindSupportedFormat(physicalDevice, new[]
                {
                    Format.D32SFloat, Format.D32SFloatS8UInt, Format.D24UNormS8UInt
                },
                ImageTiling.Optimal,
                FormatFeatureFlags.DepthStencilAttachment
                //VK_FORMAT_FEATURE_DEPTH_STENCIL_ATTACHMENT_BIT
            );
        }

        private static Format FindSupportedFormat(PhysicalDevice physicalDevice, IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features)
        {
            foreach (var format in candidates)
            {
                var props = physicalDevice.GetFormatProperties(format);

                switch (tiling)
                {
                    case ImageTiling.Linear when (props.LinearTilingFeatures & features) == features:
                        return format;
                    case ImageTiling.Optimal when (props.OptimalTilingFeatures & features) == features:
                        return format;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(tiling), tiling, "failed to find supported format!");
                }
            }

            throw new ArgumentOutOfRangeException(nameof(candidates), candidates, "failed to find supported format!");
        }

        private static readonly ConsoleRolBlock DebugReportBlock = new(5, nameof(DebugReportBlock));

        private static readonly DebugReportCallbackDelegate DebugReportDelegate = (flags, objectType, o, location, messageCode, layerPrefix, message, userData) =>
        {
            DebugReportBlock.WriteNext($"[{flags}] ({objectType}) {layerPrefix}");
            DebugReportBlock.WriteNext(message);

            return false;
        };

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
#if DEBUG

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
            AddAvailableLayer("VK_LAYER_RENDERDOC_Capture");
#endif

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

            return (instance, debugReportCallback);
        }

        public static bool HasStencilComponent(this Format format)
        {
            return format == Format.D32SFloatS8UInt || format == Format.D24UNormS8UInt;
        }

        public static AImage CreateDepthImage(this PhysicalDevice device, ImageSystem imageSystem, Canvas canvas)
        {
            return imageSystem.CreateManagedImage(device.FindDepthFormat(), ImageAspectFlags.Depth, canvas);
        }
    }
}
