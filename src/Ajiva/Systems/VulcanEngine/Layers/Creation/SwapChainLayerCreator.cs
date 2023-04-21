using Ajiva.Components.Media;
using Ajiva.Systems.VulcanEngine.Layers.Models;
using Ajiva.Systems.VulcanEngine.Systems;
using SharpVk;
using SharpVk.Khronos;

namespace Ajiva.Systems.VulcanEngine.Layers.Creation;

public static class SwapChainLayerCreator
{
    public static SwapChainLayer DefaultChecked(DeviceSystem deviceSystem, Canvas canvas)
    {
        if (deviceSystem.Device is null || deviceSystem.PhysicalDevice is null) throw new NotInitializedException(nameof(deviceSystem), deviceSystem);
        return Default(deviceSystem, canvas);
    }

    public static SwapChainLayer Default(DeviceSystem deviceSystem, Canvas canvas)
    {
        var swapChainSupport = deviceSystem.PhysicalDevice!.QuerySwapChainSupport(canvas.SurfaceHandle);
        var extent = swapChainSupport.Capabilities.ChooseSwapExtent(canvas.Extent);
        var surfaceFormat = swapChainSupport.Formats.ChooseSwapSurfaceFormat();

        var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
        if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount) 
            imageCount = swapChainSupport.Capabilities.MaxImageCount;

        var queueFamilies = deviceSystem.PhysicalDevice!.FindQueueFamilies(canvas);

        var queueFamilyIndices = queueFamilies.Indices.ToArray();

        var swapChain = deviceSystem.Device!.CreateSwapchain(canvas.SurfaceHandle,
            imageCount,
            surfaceFormat.Format,
            surfaceFormat.ColorSpace,
            extent,
            1,
            ImageUsageFlags.ColorAttachment,
            queueFamilyIndices.Length == 1
                ? SharingMode.Exclusive
                : SharingMode.Concurrent,
            queueFamilyIndices,
            swapChainSupport.Capabilities.CurrentTransform,
            CompositeAlphaFlags.Opaque,
            swapChainSupport.PresentModes.ChooseSwapPresentMode(),
            true,
            null); //todo take old swapchain

        var swapChainImage = swapChain.GetImages().Select(x => new AImage(false)
        {
            Image = x,
            View = x.CreateImageView(deviceSystem.Device!, surfaceFormat.Format, ImageAspectFlags.Color)
        }).ToArray();

        return new SwapChainLayer(surfaceFormat.Format, canvas, swapChain, swapChainImage);
    }
}
