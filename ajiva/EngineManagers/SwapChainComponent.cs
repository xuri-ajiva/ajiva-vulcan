using System;
using System.Collections.Generic;
using System.Linq;
using ajiva.Engine;
using ajiva.Models;
using SharpVk;
using SharpVk.Khronos;

namespace ajiva.EngineManagers
{
    public class SwapChainComponent : RenderEngineComponent
    {

        public SwapChainComponent(IRenderEngine renderEngine) : base(renderEngine)
        {
            SwapChain = null!;
            SwapChainImage = Array.Empty<AImage>();
            FrameBuffers = Array.Empty<Framebuffer>();
        }

        public Swapchain? SwapChain { get; private set; }
        public Format SwapChainFormat { get; private set; }
        public Extent2D SwapChainExtent { get; private set; }
        public AImage[] SwapChainImage { get; private set; }

        public Framebuffer[] FrameBuffers { get; private set; }

        public PresentMode ChooseSwapPresentMode(IEnumerable<PresentMode> availablePresentModes)
        {
            return availablePresentModes.Contains(PresentMode.Mailbox)
                ? PresentMode.Mailbox
                : PresentMode.Fifo;
        }

        public Extent2D ChooseSwapExtent(SurfaceCapabilities capabilities)
        {
            if (capabilities.CurrentExtent.Width != uint.MaxValue)
            {
                return capabilities.CurrentExtent;
            }
            return new()
            {
                Width = Math.Max(capabilities.MinImageExtent.Width, Math.Min(capabilities.MaxImageExtent.Width, RenderEngine.Window.SurfaceWidth)),
                Height = Math.Max(capabilities.MinImageExtent.Height, Math.Min(capabilities.MaxImageExtent.Height, RenderEngine.Window.SurfaceHeight))
            };
        }

        public SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice device)
        {
            return new()
            {
                Capabilities = device.GetSurfaceCapabilities(RenderEngine.Window.Surface),
                Formats = device.GetSurfaceFormats(RenderEngine.Window.Surface),
                PresentModes = device.GetSurfacePresentModes(RenderEngine.Window.Surface)
            };
        }

        public SurfaceFormat ChooseSwapSurfaceFormat(SurfaceFormat[] availableFormats)
        {
            if (availableFormats.Length == 1 && availableFormats[0].Format == Format.Undefined)
            {
                return new()
                {
                    Format = Format.B8G8R8A8UNorm,
                    ColorSpace = ColorSpace.SrgbNonlinear
                };
            }

            foreach (var format in availableFormats)
            {
                if (format.Format == Format.B8G8R8A8UNorm && format.ColorSpace == ColorSpace.SrgbNonlinear)
                {
                    return format;
                }
            }

            return availableFormats[0];
        }

        public struct SwapChainSupportDetails
        {
            public SurfaceCapabilities Capabilities;
            public SurfaceFormat[] Formats;
            public PresentMode[] PresentModes;
        }

        public uint AcquireNextImage()
        {
            ATrace.Assert(SwapChain != null, nameof(SwapChain) + " != null");
            return SwapChain.AcquireNextImage(uint.MaxValue, RenderEngine.SemaphoreComponent.ImageAvailable, null);
        }

        public void CreateSwapChain()
        {
            var swapChainSupport = QuerySwapChainSupport(RenderEngine.DeviceComponent.PhysicalDevice);

            var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
            if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount)
            {
                imageCount = swapChainSupport.Capabilities.MaxImageCount;
            }

            var surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);

            var queueFamilies = RenderEngine.DeviceComponent.FindQueueFamilies(RenderEngine.DeviceComponent.PhysicalDevice);

            var queueFamilyIndices = queueFamilies.Indices.ToArray();

            var extent = ChooseSwapExtent(swapChainSupport.Capabilities);

            SwapChain = RenderEngine.DeviceComponent.Device.CreateSwapchain(RenderEngine.Window.Surface,
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
                ChooseSwapPresentMode(swapChainSupport.PresentModes),
                true,
                SwapChain);

            SwapChainImage = SwapChain.GetImages().Select(x => new AImage(false)
            {
                Image = x
            }).ToArray();
            SwapChainFormat = surfaceFormat.Format;
            SwapChainExtent = extent;
        }

        public void CreateImageViews()
        {
            foreach (var image in SwapChainImage)
            {
                image.View = RenderEngine.ImageComponent.CreateImageView(image.Image, SwapChainFormat, ImageAspectFlags.Color);
            }
        }

        public void CreateFrameBuffers()
        {
            Framebuffer Create(ImageView imageView) => RenderEngine.DeviceComponent.Device.CreateFramebuffer(RenderEngine.GraphicsComponent.RenderPass,
                new[]
                {
                    imageView, RenderEngine.ImageComponent.DepthImage.View
                },
                SwapChainExtent.Width,
                SwapChainExtent.Height,
                1);

            FrameBuffers = SwapChainImage.Select(x => Create(x.View)).ToArray();
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            SwapChain?.Dispose();
            foreach (var frameBuffer in FrameBuffers)
            {
                frameBuffer.Dispose();
            }
            foreach (var image in SwapChainImage)
            {
                image.Dispose();
            }
        }

        public void CleanupSwapChain()
        {
            foreach (var frameBuffer in FrameBuffers)
                frameBuffer.Dispose();
            FrameBuffers = Array.Empty<Framebuffer>();

            foreach (var aImage in SwapChainImage)
            {
                aImage.Dispose();
            }
            SwapChainImage = Array.Empty<AImage>();

            SwapChain?.Dispose();
            SwapChain = null;
        }
    }
}
