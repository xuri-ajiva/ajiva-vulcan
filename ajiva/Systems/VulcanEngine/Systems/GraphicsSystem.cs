using System.Collections.Generic;
using System.Linq;
using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Ecs;
using ajiva.Ecs.System;
using ajiva.Ecs.Utils;
using ajiva.Systems.VulcanEngine.Ui;
using ajiva.Systems.VulcanEngine.Unions;
using ajiva.Utils;
using ajiva.Utils.Changing;

namespace ajiva.Systems.VulcanEngine.Systems
{
    [Dependent(typeof(TextureSystem), typeof(Ajiva3dSystem), typeof(UiRenderer))]
    public class GraphicsSystem : SystemBase, IInit, IUpdate
    {
        private readonly DeviceSystem deviceSystem;
        private readonly ImageSystem imageSystem;
        private readonly WindowSystem windowSystem;
        private readonly ShaderSystem shaderSystem;
        private readonly TextureSystem textureSystem;
        private readonly Ajiva3dSystem ajiva3dSystem;
        private readonly UiRenderer uiRenderer;
        public IChangingObserver ChangingObserver { get; } = new ChangingObserver(100);

        private static readonly object CurrentGraphicsLayoutSwapLock = new();

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            renderUnion?.Dispose();
        }

        /// <inheritdoc />
        public void Init(AjivaEcs ecs)
        {
            RecreateSwapchain();
            Ecs.GetSystem<WindowSystem>().OnResize += OnResize;
        }

        private void RecreateSwapchain()
        {
            RecreateSwapchainCore();
        }

        private void OnResize()
        {
            RecreateSwapchain();
        }

        /// <inheritdoc />
        public void Update(UpdateInfo delta)
        {
            if (ChangingObserver.UpdateCycle(delta.Iteration))
            {
                RecreateSwapchainCore();
                //RefillFrameBuffers();
            }
            DrawFrame();
        }

        private void RefillFrameBuffers()
        {
            ChangingObserver.Updated();

            //renderUnion.FillFrameBuffers(ar.ComponentEntityMap.Keys.Union<ARenderAble>(ui.ComponentEntityMap.Keys));
            renderUnion?.FillFrameBuffers(new Dictionary<AjivaVulkanPipeline, List<ARenderAble>>()
            {
                [AjivaVulkanPipeline.Pipeline2d] = ajiva3dSystem.ComponentEntityMap.Keys.Cast<ARenderAble>().ToList(),
                [AjivaVulkanPipeline.Pipeline3d] = uiRenderer.ComponentEntityMap.Keys.Cast<ARenderAble>().ToList(),
            });
        }

        /// <inheritdoc />
        public GraphicsSystem(AjivaEcs ecs, DeviceSystem deviceSystem, ImageSystem imageSystem, WindowSystem windowSystem, ShaderSystem shaderSystem, TextureSystem textureSystem, Ajiva3dSystem ajiva3dSystem, UiRenderer uiRenderer) : base(ecs)
        {
            this.deviceSystem = deviceSystem;
            this.imageSystem = imageSystem;
            this.windowSystem = windowSystem;
            this.shaderSystem = shaderSystem;
            this.textureSystem = textureSystem;
            this.ajiva3dSystem = ajiva3dSystem;
            this.uiRenderer = uiRenderer;
        }

        private void RecreateSwapchainCore()
        {
            lock (CurrentGraphicsLayoutSwapLock)
            {
                renderUnion?.Dispose();
                depthImage?.Dispose();

                depthImage = deviceSystem.PhysicalDevice.CreateDepthImage(imageSystem, windowSystem.Canvas);

                renderUnion = RenderUnion.CreateRenderUnion(deviceSystem.PhysicalDevice, deviceSystem.Device!, windowSystem.Canvas, shaderSystem, textureSystem.TextureSamplerImageViews, true, depthImage, deviceSystem.CommandPool);

                RefillFrameBuffers();
            }
        }

        private RenderUnion? renderUnion;
        private AImage? depthImage;

        public void DrawFrame()
        {
            lock (CurrentGraphicsLayoutSwapLock)
            {
                renderUnion.DrawFrame(deviceSystem.GraphicsQueue, deviceSystem.PresentQueue);
            }
        }
    }
}
