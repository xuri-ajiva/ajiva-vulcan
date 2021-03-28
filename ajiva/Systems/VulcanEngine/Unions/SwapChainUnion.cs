using System.Linq;
using ajiva.Components.Media;
using ajiva.Models;
using ajiva.Utils;
using SharpVk;
using SharpVk.Khronos;

namespace ajiva.Systems.VulcanEngine.Unions
{
    public class SwapChainUnion : DisposingLogger
    {
        public Format SwapChainFormat { get; }
        public Canvas Canvas { get; }
        public Swapchain SwapChain { get; }
        public AImage[] SwapChainImage { get; }

        public SwapChainUnion(Format swapChainFormat, Canvas canvas, Swapchain swapChain, AImage[] swapChainImage)
        {
            SwapChainFormat = swapChainFormat;
            Canvas = canvas;
            SwapChain = swapChain;
            SwapChainImage = swapChainImage;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            SwapChain.Dispose();
            foreach (var aImage in SwapChainImage)
            {
                aImage.Dispose();
            }
        }

        public static SwapChainUnion CreateSwapChainUnion(PhysicalDevice physicalDevice, Device device, Canvas canvas)
        {
            var swapChainSupport = physicalDevice.QuerySwapChainSupport(canvas.SurfaceHandle);
            var extent = swapChainSupport.Capabilities.ChooseSwapExtent(canvas.Extent);
            var surfaceFormat = swapChainSupport.Formats.ChooseSwapSurfaceFormat();

            var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
            if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount)
            {
                imageCount = swapChainSupport.Capabilities.MaxImageCount;
            }

            var queueFamilies = physicalDevice.FindQueueFamilies(canvas);

            var queueFamilyIndices = queueFamilies.Indices.ToArray();

            Swapchain swapChain = device.CreateSwapchain(canvas.SurfaceHandle,
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
                null);

            AImage[] swapChainImage = swapChain.GetImages().Select(x => new AImage(false)
            {
                Image = x,
                View = x.CreateImageView(device, surfaceFormat.Format, ImageAspectFlags.Color)
            }).ToArray();

            return new(surfaceFormat.Format, canvas, swapChain, swapChainImage);
        }
    }
}
