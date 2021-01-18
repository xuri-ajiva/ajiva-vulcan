using System;
using System.Collections.Generic;
using System.Linq;
using ajiva.Engine;
using ajiva.Helpers;
using ajiva.Models;
using SharpVk;
using SharpVk.Khronos;

namespace ajiva.EngineManagers
{
    public class SwapChainComponent : RenderEngineComponent
    {
        public SwapChainComponent(IRenderEngine renderEngine) : base(renderEngine)
        {
        }

        public Swapchain? SwapChain { get; private set; }
        public Format? SwapChainFormat { get; private set; }
        public Extent2D? SwapChainExtent { get; private set; }
        public AImage[]? SwapChainImage { get; private set; }

        public Framebuffer[]? FrameBuffers { get; private set; }

#region Choise

        public static PresentMode ChooseSwapPresentMode(IEnumerable<PresentMode> availablePresentModes)
        {
            return availablePresentModes.Contains(PresentMode.Mailbox)
                ? PresentMode.Mailbox
                : PresentMode.Fifo;
        }

        public static SurfaceFormat ChooseSwapSurfaceFormat(SurfaceFormat[] availableFormats)
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

  #endregion
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

        public void EnsureSwapChainExists()
        {
            RenderEngine.DeviceComponent.EnsureDevicesExist();

            var swapChainSupport = QuerySwapChainSupport(RenderEngine.DeviceComponent.PhysicalDevice!);

            var extent = ChooseSwapExtent(swapChainSupport.Capabilities);
            var surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);

            if (SwapChain == null) CreateSwapChain(swapChainSupport, surfaceFormat, extent);
            if (SwapChainImage == null) CreateSwapchainImages();

            SwapChainFormat ??= surfaceFormat.Format;
            SwapChainExtent ??= extent;

            //directly create the views sow we dont forget it, and reduce dependency
            if (SwapChainImage!.Any(x => x.View == null)) CreateImageViews();
        }

        private void CreateSwapChain(SwapChainSupportDetails swapChainSupport, SurfaceFormat surfaceFormat, Extent2D extent)
        {
            var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
            if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount)
            {
                imageCount = swapChainSupport.Capabilities.MaxImageCount;
            }

            var queueFamilies = RenderEngine.DeviceComponent.FindQueueFamilies(RenderEngine.DeviceComponent.PhysicalDevice);

            var queueFamilyIndices = queueFamilies.Indices.ToArray();

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
        }

        private void CreateSwapchainImages()
        {
            SwapChainImage = SwapChain!.GetImages().Select(x => new AImage(false)
            {
                Image = x
            }).ToArray();
        }

        private void CreateImageViews()
        {
            foreach (var image in SwapChainImage!)
            {
                image.View ??= RenderEngine.ImageComponent.CreateImageView(image.Image, SwapChainFormat!.Value, ImageAspectFlags.Color);
            }
        }

        public void EnsureFrameBuffersExists()
        {
            RenderEngine.ImageComponent.EnsureDepthResourcesExits();
            RenderEngine.GraphicsComponent.EnsureGraphicsLayoutExists();
            RenderEngine.GraphicsComponent.Current!.EnsureExists();

            Framebuffer Create(ImageView imageView) => RenderEngine.DeviceComponent.Device!.CreateFramebuffer(RenderEngine.GraphicsComponent.Current.RenderPass,
                new[]
                {
                    imageView, RenderEngine.ImageComponent.DepthImage!.View
                },
                SwapChainExtent!.Value.Width,
                SwapChainExtent!.Value.Height,
                1);

            FrameBuffers ??= SwapChainImage!.Select(x => Create(x.View!)).ToArray();
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            EnsureSwapChainDeletion();
        }

        public void EnsureSwapChainDeletion()
        {
            if (FrameBuffers != null)
                foreach (var frameBuffer in FrameBuffers)
                    frameBuffer.Dispose();
            FrameBuffers = null;

            if (SwapChainImage != null)
                foreach (var aImage in SwapChainImage)
                {
                    aImage.Dispose();
                }
            SwapChainImage = null;

            SwapChain?.Dispose();
            SwapChain = null;

            SwapChainExtent = null;
            SwapChainFormat = null;
        }
    }
}
