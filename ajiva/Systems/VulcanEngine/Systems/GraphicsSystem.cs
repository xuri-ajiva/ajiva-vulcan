using System;
using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Ecs;
using ajiva.Ecs.System;
using ajiva.Ecs.Utils;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Unions;
using ajiva.Utils;
using ajiva.Utils.Changing;
using Ajiva.Wrapper.Logger;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Systems
{
    [Dependent(typeof(TextureSystem), typeof(Ajiva3dSystem), typeof(UiRenderer))]
    public class GraphicsSystem : SystemBase, IInit, IUpdate
    {
        public IChangingObserver ChangingObserver { get; } = new ChangingObserver(100);

        private static readonly object CurrentGraphicsLayoutSwapLock = new();

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            DepthImage?.Dispose();
            DepthImage = null!;
            renderUnion?.Dispose();
            renderUnion = null!;
        }

        public void RecreateCurrentGraphicsLayout()
        {
            lock (CurrentGraphicsLayoutSwapLock)
            {
                deviceSystem.WaitIdle();
                ChangingObserver.Updated();

                ReCreateRenderUnion();
            }
        }

        /// <inheritdoc />
        public void Init(AjivaEcs ecs)
        {
            ResolveDeps();
            RecreateCurrentGraphicsLayout();
            windowSystem.OnResize += RecreateCurrentGraphicsLayout;
        }

        /// <inheritdoc />
        public void Update(UpdateInfo delta)
        {
            if (ChangingObserver.UpdateCycle(delta.Iteration))
                UpdateGraphicsData();
            lock (CurrentGraphicsLayoutSwapLock)
                DrawFrame();
        }

        /// <inheritdoc />
        public GraphicsSystem(AjivaEcs ecs) : base(ecs)
        {
        }

        #region gl

        private AImage DepthImage { get; set; }

        private Format DepthFormat { get; set; }

        private RenderUnion renderUnion;

        private Queue render;
        private Queue presentation;

        public void DrawFrame()
        {
            renderUnion.DrawFrame(render, presentation);
        }

        private DeviceSystem deviceSystem;
        private ImageSystem imageSystem;
        private WindowSystem windowSystem;
        private ShaderSystem shaderSystem;
        private TextureSystem textureSystem;
        private Ajiva3dSystem ajiva3dSystem;
        private UiRenderer uiRenderer;
        private const int DISPOSE_DALEY = 1000;

        public void ResolveDeps()
        {
            deviceSystem = Ecs.GetSystem<DeviceSystem>();
            imageSystem = Ecs.GetComponentSystem<ImageSystem, AImage>();
            windowSystem = Ecs.GetSystem<WindowSystem>();
            shaderSystem = Ecs.GetSystem<ShaderSystem>();
            textureSystem = Ecs.GetComponentSystem<TextureSystem, ATexture>();
            ajiva3dSystem = Ecs.GetComponentSystem<Ajiva3dSystem, ARenderAble3D>();
            uiRenderer = Ecs.GetComponentSystem<UiRenderer, ARenderAble2D>();

            render = deviceSystem.GraphicsQueue!;
            presentation = deviceSystem.PresentQueue!;
            DepthFormat = (deviceSystem.PhysicalDevice ?? throw new InvalidOperationException()).FindDepthFormat();
        }

        protected void ReCreateRenderUnion()
        {
            ReCreateDepthImage();

            renderUnion?.DisposeIn(DISPOSE_DALEY);
            deviceSystem.UseCommandPool(x =>
            {
                renderUnion = RenderUnion.CreateRenderUnion(
                    deviceSystem.PhysicalDevice ?? throw new InvalidOperationException(),
                    deviceSystem.Device!,
                    windowSystem.Canvas,
                    shaderSystem,
                    textureSystem.TextureSamplerImageViews,
                    true,
                    DepthImage!,
                    x
                );
            });
            UpdateGraphicsData();
        }

        private void UpdateGraphicsData()
        {
            LogHelper.Log("Updating BufferData");
            ChangingObserver.Updated();
            //renderUnion.FillFrameBuffers(ar.ComponentEntityMap.Keys.Union<ARenderAble>(ui.ComponentEntityMap.Keys));
            renderUnion.FillFrameBuffers(new Dictionary<AjivaVulkanPipeline, List<ARenderAble>>()
            {
                [AjivaVulkanPipeline.Pipeline2d] = uiRenderer.ComponentEntityMap.Keys.Cast<ARenderAble>().ToList(),
                [AjivaVulkanPipeline.Pipeline3d] = ajiva3dSystem.ComponentEntityMap.Keys.Cast<ARenderAble>().ToList(),
            });
        }

        private void ReCreateDepthImage()
        {
            DepthImage?.DisposeIn(DISPOSE_DALEY);
            DepthImage = imageSystem.CreateManagedImage(DepthFormat, ImageAspectFlags.Depth, windowSystem.Canvas);
        }

  #endregion
    }
}
