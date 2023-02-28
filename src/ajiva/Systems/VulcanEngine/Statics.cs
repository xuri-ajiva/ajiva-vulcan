using System.Diagnostics;
using System.Text;
using ajiva.Components.Media;
using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;
using SharpVk.Multivendor;
using Version = SharpVk.Version;

namespace ajiva.Systems.VulcanEngine;

public static class Statics
{
    private static HashSet<string> Ignore = new HashSet<string>() {
        "UNASSIGNED-CoreValidation-DrawState-InvalidImageLayout",
        "VUID-VkPresentInfoKHR-pImageIndices-01296"
    };
    private static readonly DebugReportCallbackDelegate DebugReportDelegate = (flags, objectType, o, location, messageCode, layerPrefix, message, userData) =>
    {
        if (message.Contains('['))
        {
            var p0 = message.IndexOf('[') + 2;
            var p1 = message.IndexOf(']') - 1;
            var ident = message.Substring(p0, p1 - p0);
            if (Ignore.Contains(ident)) return false;
        }
        var stackframe = new StackFrame(2, true);
        var lvl = (flags & DebugReportFlags.Error) != 0
            ? ALogLevel.Error
            : (flags & DebugReportFlags.Warning) != 0
                ? ALogLevel.Warning
                : (flags & DebugReportFlags.PerformanceWarning) != 0
                    ? ALogLevel.Warning
                    : (flags & DebugReportFlags.Debug) != 0
                        ? ALogLevel.Debug
                        : ALogLevel.Info;
        
        ALog.Log(lvl, $"[{flags}] ({objectType}) {layerPrefix} #{message}");
        ALog.Log(lvl, message);
        ALog.Log(lvl, $"File: {stackframe.GetFileName()}:{stackframe.GetFileLineNumber()} " + BuildStackTraceError(3, 6));

        string BuildStackTraceError(int begin, int count)
        {
            var stackTrace = new StackTrace(begin, true);
            var stringBuilder = new StringBuilder();
            foreach (var frame in stackTrace.GetFrames().Take(count))
            {
                stringBuilder.Append($"from {frame.GetFileName()}:{frame.GetFileLineNumber()} ");
            }
            return stringBuilder.ToString();
        }

        return false;
    };

    public static ImageView CreateImageView(this Image image, Device device, Format format, ImageAspectFlags aspectFlags)
    {
        return device.CreateImageView(image, ImageViewType.ImageView2d, format, ComponentMapping.Identity, new ImageSubresourceRange {
            AspectMask = aspectFlags,
            BaseMipLevel = 0,
            LevelCount = 1,
            BaseArrayLayer = 0,
            LayerCount = 1
        });
    }

    public static ImageView CreateImageViewArray(this Image image, Device device, Format format, ImageAspectFlags aspectFlags, uint length)
    {
        return device.CreateImageView(image, ImageViewType.ImageView2dArray, format, ComponentMapping.Identity, new ImageSubresourceRange {
            AspectMask = aspectFlags,
            BaseMipLevel = 0,
            LevelCount = 1,
            BaseArrayLayer = 0,
            LayerCount = length,
        });
    }

    public static Format FindDepthFormat(this PhysicalDevice physicalDevice)
    {
        return physicalDevice.FindSupportedFormat(new[] {
                Format.D32SFloat, Format.D32SFloatS8UInt, Format.D24UNormS8UInt
            },
            ImageTiling.Optimal,
            FormatFeatureFlags.DepthStencilAttachment
            //VK_FORMAT_FEATURE_DEPTH_STENCIL_ATTACHMENT_BIT
        );
    }

    public static Format FindSupportedFormat(this PhysicalDevice physicalDevice, IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features)
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

    public static void LogStackTrace()
    {
        var stackFrame = new StackTrace(true);
        foreach (var frame in stackFrame.GetFrames()) Console.WriteLine($"{frame.GetFileName()}:{frame.GetFileLineNumber()}");
    }

    /*private static DataTarget dataTarget = DataTarget.AttachToProcess(Environment.ProcessId, false);
    private static ClrRuntime runtime = dataTarget.ClrVersions.First().CreateRuntime();*/

    public static (Instance instance, DebugReportCallback debugReportCallback) CreateInstance(IEnumerable<string> enabledExtensionNames)
    {
        //if (Instance != null) return;

        var enabledLayers = new List<string>();

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
            applicationInfo: new ApplicationInfo {
                ApplicationName = "ajiva",
                ApplicationVersion = new Version(0, 0, 1),
                EngineName = "ajiva-engine",
                EngineVersion = new Version(0, 0, 1),
                ApiVersion = new Version(1, 0, 0)
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

    public static IndexType GetIndexType(uint indexBufferSizeOfT)
    {
        return indexBufferSizeOfT switch {
            sizeof(uint) => IndexType.Uint32,
            sizeof(ushort) => IndexType.Uint16,
            sizeof(byte) => IndexType.Uint8,
            _ => throw new ArgumentOutOfRangeException(nameof(indexBufferSizeOfT), indexBufferSizeOfT, "Unknown Index Type")
        };
    }
}
