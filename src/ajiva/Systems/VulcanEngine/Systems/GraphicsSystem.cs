using System;
using System.Collections.Generic;
using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Ecs;
using ajiva.Ecs.System;
using ajiva.Ecs.Utils;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers;
using ajiva.Utils;
using ajiva.Utils.Changing;
using Ajiva.Wrapper.Logger;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Systems
{
    [Dependent(typeof(TextureSystem))]
    public class GraphicsSystem : SystemBase, IInit, IUpdate
    {
        public IOverTimeChangingObserver ChangingObserver { get; } = new OverTimeChangingObserver(100);

        public Dictionary<AjivaVulkanPipeline, IAjivaLayer> Layers { get; } = new();

        private static readonly object CurrentGraphicsLayoutSwapLock = new();

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            ajivaLayerRenderer?.Dispose();
            ajivaLayerRenderer = null!;
        }

        public void RecreateCurrentGraphicsLayout()
        {
            lock (CurrentGraphicsLayoutSwapLock)
            {
                deviceSystem.WaitIdle();

                ReCreateRenderUnion();
            }
        }

        private bool recreateCurrentGraphicsLayoutNeeded = true;

        /// <inheritdoc />
        public void Init(IAjivaEcs ecs)
        {
            ResolveDeps();
            //RecreateCurrentGraphicsLayout();
            ChangingObserver.Changed();
            //ChangingObserver.OnUpdate += _ => UpdateGraphicsData();
            windowSystem.OnResize += WindowResized;
        }

        private void WindowResized()
        {
            RecreateCurrentGraphicsLayout();
        }

        /// <inheritdoc />
        public void Update(UpdateInfo delta)
        {
            if (recreateCurrentGraphicsLayoutNeeded)
            {
                RecreateCurrentGraphicsLayout();
                recreateCurrentGraphicsLayoutNeeded = false;
            }
            if (ChangingObserver.UpdateCycle(delta.Iteration))
                UpdateGraphicsData();
            lock (CurrentGraphicsLayoutSwapLock)
                DrawFrame();
        }

        /// <inheritdoc />
        public GraphicsSystem(IAjivaEcs ecs) : base(ecs)
        {
        }

#region gl

        private Format DepthFormat { get; set; }

        private AjivaLayerRenderer ajivaLayerRenderer;

        public void DrawFrame()
        {
            var render = deviceSystem.GraphicsQueue!;
            var presentation = deviceSystem.PresentQueue!;
            lock (presentation)
            {
                lock (render)
                {
                    ajivaLayerRenderer.DrawFrame(render, presentation);
                }
            }
        }

        private DeviceSystem deviceSystem;
        private WindowSystem windowSystem;

        public void ResolveDeps()
        {
            deviceSystem = Ecs.GetSystem<DeviceSystem>();
            windowSystem = Ecs.GetSystem<WindowSystem>();

            DepthFormat = (deviceSystem.PhysicalDevice ?? throw new InvalidOperationException()).FindDepthFormat();
        }

        private const int DISPOSE_DALEY = 1000;

        protected void ReCreateRenderUnion()
        {
            ajivaLayerRenderer?.DisposeIn(DISPOSE_DALEY);

            var imageAvailable = deviceSystem.Device!.CreateSemaphore()!;
            var renderFinished = deviceSystem.Device!.CreateSemaphore()!;

            ajivaLayerRenderer = new AjivaLayerRenderer(imageAvailable, renderFinished);

            ajivaLayerRenderer.PrepareRenderSubmitInfo(deviceSystem, windowSystem.Canvas, Layers);

            UpdateGraphicsData();
        }

        private void UpdateGraphicsData()
        {
            //LogHelper.Log("Updating BufferData");
                ChangingObserver.Updated();
            ajivaLayerRenderer.FillBuffers();
            /*foreach (var (_, layer) in layerSystem.Layers)
            {
                renderUnion.FillFrameBuffer(layer.PipelineLayer, layer.GetRenders(), meshPool);
            }*/
        }

#endregion

        public void AddUpdateLayer(IAjivaLayer layer)
        {
            Layers.Add(layer.PipelineLayer, layer);
        }
    }
}
