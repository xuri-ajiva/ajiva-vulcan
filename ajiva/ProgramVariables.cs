using System;
using System.Diagnostics;
using SharpVk;
using SharpVk.Multivendor;

namespace ajiva
{
    public partial class Program : IDisposable
    {
        #region Vars

        private const int SurfaceWidth = 800;
        private const int SurfaceHeight = 600;
        private static readonly DebugReportCallbackDelegate DebugReportDelegate = DebugReport;

        private long initialTimestamp;

        #endregion

        private static Bool32 DebugReport(DebugReportFlags flags, DebugReportObjectType objectType, ulong @object, HostSize location, int messageCode, string layerPrefix, string message, IntPtr userData)
        {
            Debug.WriteLine(message);
            Console.WriteLine(message);

            return false;
        }

        private void Cleanup()
        {
            Dispose();
        }

        private void CleanupSwapChain()
        {
            ImageManager.DepthImage.Dispose();

            SwapChainManager.CleanupSwapChain();

            DeviceManager.FreeCommandBuffers();

            GraphicsManager.Pipeline.Dispose();

            GraphicsManager.PipelineLayout.Dispose();

            GraphicsManager.RenderPass.Dispose();

            GraphicsManager.DescriptorPool.Dispose();

            /*     depthImageView.Dispose();
                 depthImage.Dispose();
                 depthImageMemory.Free();
     
                 if (frameBuffers != null)
                     foreach (var frameBuffer in frameBuffers)
                         frameBuffer.Dispose();
                 frameBuffers = null;
     
                 commandPool?.FreeCommandBuffers(commandBuffers);
     
                 pipeline?.Dispose();
                 pipeline = null;
     
                 pipelineLayout?.Dispose();
                 pipelineLayout = null;
     
                 renderPass?.Dispose();
                 renderPass = null;
     
                 swapChain?.Dispose();
                 swapChain = null;
     
                 if (swapChainImageViews != null)
                     foreach (var imageView in swapChainImageViews)
                         imageView.Dispose();
                 swapChainImageViews = null;
     
                 descriptorPool?.Dispose();        */
        }

        private void RecreateSwapChain()
        {
            DeviceManager.WaitIdle();
            CleanupSwapChain();

            SwapChainManager.CreateSwapChain();
            SwapChainManager.CreateImageViews();
            GraphicsManager.CreateRenderPass();
            GraphicsManager.CreateGraphicsPipeline();
            ImageManager.CreateDepthResources();
            SwapChainManager.CreateFrameBuffers();
            //bufferManager.CreateUniformBuffer();   free in cleanup swapchain if created here
            GraphicsManager.CreateDescriptorPool();
            GraphicsManager.CreateDescriptorSet();
            DeviceManager.CreateCommandBuffers();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            SwapChainManager.Dispose();
            Window.Dispose();
            ImageManager.Dispose();
            GraphicsManager.Dispose();
            ShaderManager.Dispose();
            BufferManager.Dispose();
            SemaphoreManager.Dispose();
            TextureManager.Dispose();
            DeviceManager.Dispose();
            Instance?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
