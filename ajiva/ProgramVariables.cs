using System;
using System.Diagnostics;
using SharpVk;
using SharpVk.Multivendor;

namespace ajiva
{
    public partial class Program
    {
        #region Vars

        private const int SurfaceWidth = 800;
        private const int SurfaceHeight = 600;
        private static readonly DebugReportCallbackDelegate DebugReportDelegate = DebugReport;

        private long initialTimestamp;

        #endregion

        private static Bool32 DebugReport(DebugReportFlags flags, DebugReportObjectType objectType, ulong @object, HostSize location, int messageCode, string layerPrefix, string message, IntPtr userData)
        {
            Console.WriteLine($"[{flags}] ({objectType}) {layerPrefix}:\n{message}");

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
            GC.SuppressFinalize(this);
            if (!Runing) return;
            Runing = false;

            DeviceManager.WaitIdle();
            
            SwapChainManager.Dispose();
            ImageManager.Dispose();
            GraphicsManager.Dispose();
            ShaderManager.Dispose();
            BufferManager.Dispose();
            SemaphoreManager.Dispose();
            TextureManager.Dispose();
            Window.Dispose();
            DeviceManager.Dispose();
            Runing = false;
        }
    }
}
