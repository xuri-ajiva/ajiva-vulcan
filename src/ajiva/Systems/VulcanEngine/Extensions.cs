using System.Runtime.CompilerServices;
using ajiva.Models;
using SharpVk;
using SharpVk.Khronos;

namespace ajiva.Systems.VulcanEngine;

public static class Extensions
{
    public static object VKdeviceLock = new object();

    public static SwapChainSupportDetails QuerySwapChainSupport(this PhysicalDevice device, Surface surface)
    {
        lock (VKdeviceLock)
        {
            return new SwapChainSupportDetails
            {
                Capabilities = device.GetSurfaceCapabilities(surface),
                Formats = device.GetSurfaceFormats(surface),
                PresentModes = device.GetSurfacePresentModes(surface)
            };
        }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static Extent2D ChooseSwapExtent(this SurfaceCapabilities capabilities, Extent2D surfaceExtent)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue) return capabilities.CurrentExtent;
        return new Extent2D
        {
            Width = Math.Max(capabilities.MinImageExtent.Width, Math.Min(capabilities.MaxImageExtent.Width, surfaceExtent.Width)),
            Height = Math.Max(capabilities.MinImageExtent.Height, Math.Min(capabilities.MaxImageExtent.Height, surfaceExtent.Height))
        };
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static PresentMode ChooseSwapPresentMode(this IEnumerable<PresentMode> availablePresentModes)
    {
        return availablePresentModes.Contains(PresentMode.Mailbox)
            ? PresentMode.Mailbox
            : PresentMode.Fifo;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static SurfaceFormat ChooseSwapSurfaceFormat(this SurfaceFormat[] availableFormats)
    {
        if (availableFormats.Length == 1 && availableFormats[0].Format == Format.Undefined)
            return new SurfaceFormat
            {
                Format = Format.B8G8R8A8UNorm,
                ColorSpace = ColorSpace.SrgbNonlinear
            };

        foreach (var format in availableFormats)
            if (format.Format == Format.B8G8R8A8UNorm && format.ColorSpace == ColorSpace.SrgbNonlinear)
                return format;

        return availableFormats[0];
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static QueueFamilyIndices FindQueueFamilies(this PhysicalDevice device, Canvas canvas)
    {
        var indices = new QueueFamilyIndices();
        lock (VKdeviceLock)
        {
            var queueFamilies = device.GetQueueFamilyProperties();

            for (uint index = 0; index < queueFamilies.Length && !indices.IsComplete; index++)
            {
                if (queueFamilies[index].QueueFlags.HasFlag(QueueFlags.Graphics)) indices.GraphicsFamily = index;

                if (device.GetSurfaceSupport(index, canvas.SurfaceHandle)) indices.PresentFamily = index;

                if (queueFamilies[index].QueueFlags.HasFlag(QueueFlags.Transfer) && !queueFamilies[index].QueueFlags.HasFlag(QueueFlags.Graphics)) indices.TransferFamily = index;
            }

            indices.TransferFamily ??= indices.GraphicsFamily;
        }
        return indices;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static bool IsSuitableDevice(this PhysicalDevice dvc, Canvas surface)
    {
        var features = dvc.GetFeatures();

        return dvc.EnumerateDeviceExtensionProperties(null).Any(extension => extension.ExtensionName == KhrExtensions.Swapchain)
               && FindQueueFamilies(dvc, surface).IsComplete && features.SamplerAnisotropy;
    }

    public struct SwapChainSupportDetails
    {
        public SurfaceCapabilities Capabilities;
        public SurfaceFormat[] Formats;
        public PresentMode[] PresentModes;
    }
}