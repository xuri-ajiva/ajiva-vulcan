using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ajiva.EngineManagers;
using ajiva.Models;
using SharpVk;
using SharpVk.Khronos;
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
            Debug.WriteLine(message);
            Console.WriteLine(message);

            return false;
        }

        private void Cleanup()
        {
            /*
                    device?.WaitIdle();
                    CleanupSwapChain();
        
                    textureSampler.Dispose();
                    textureImageView.Dispose();
        
                    textureImage.Dispose();
                    textureImageMemory.Free();
        
                    descriptorSetLayout?.Dispose();
                    descriptorSetLayout = null;
        
                    bufferManager.Dispose();
        
                    renderFinished?.Dispose();
                    renderFinished = null;
        
                    imageAvailable?.Dispose();
                    imageAvailable = null;
        
                    descriptorPool?.Dispose();
                    descriptorPool = null;
                    descriptorSet = null;
        
                    commandPool?.Dispose();
                    commandPool = null;
                    commandBuffers = null;
        
                    fragShader?.Dispose();
                    fragShader = null;
        
                    vertShader?.Dispose();
                    vertShader = null;
        
                    device?.Dispose();
                    device = null;
        
                    window?.Dispose();
                    window = null;
        
                    Instance?.Dispose();
                    Instance = null;
        
                    window.CloseWindow();      */
        }

        private void CleanupSwapChain()
        {
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
            /*device?.WaitIdle();
            CleanupSwapChain();

            CreateSwapChain();
            CreateImageViews();
            CreateRenderPass();
            CreateGraphicsPipeline();
            CreateDepthResources();
            CreateFrameBuffers();
            //bufferManager.CreateUniformBuffer();   free in cleanup swapchain if created here
            CreateDescriptorPool();
            CreateDescriptorSet();
            CreateCommandBuffers();  */
        }
    }
}
