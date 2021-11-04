using System;
using System.Collections.Generic;
using System.Linq;
using ajiva.Ecs;
using ajiva.Ecs.System;
using ajiva.Ecs.Utils;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers;
using ajiva.Utils;
using ajiva.Utils.Changing;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Systems
{
    [Dependent(typeof(TextureSystem))]
    public class GraphicsSystem : SystemBase, IInit, IUpdate
    {
        private static readonly object CurrentGraphicsLayoutSwapLock = new object();

        private AjivaLayerRenderer? ajivaLayerRenderer;

        private DeviceSystem deviceSystem;

        private bool reInitAjivaLayerRendererNeeded = true;
        private WindowSystem windowSystem;

        /// <inheritdoc />
        public GraphicsSystem(IAjivaEcs ecs) : base(ecs)
        {
        }

        public IOverTimeChangingObserver ChangingObserver { get; } = new OverTimeChangingObserver(100);

        public Dictionary<AjivaVulkanPipeline, IAjivaLayer> Layers { get; } = new Dictionary<AjivaVulkanPipeline, IAjivaLayer>();

        private Format DepthFormat { get; set; }

        private ISet<IAjivaLayerRenderSystem> ToUpdate { get; } = new HashSet<IAjivaLayerRenderSystem>();

        /// <inheritdoc />
        public void Init()
        {
            ResolveDeps();
            windowSystem.OnResize += WindowResized;
        }

        /// <inheritdoc />
        public void Update(UpdateInfo delta)
        {
            if (reInitAjivaLayerRendererNeeded || ajivaLayerRenderer is null)
            {
                RecreateCurrentGraphicsLayout();
                reInitAjivaLayerRendererNeeded = false;
            }

            if (!ChangingObserver.Locked && ToUpdate.Any())
            {
                foreach (var renderSystem in ToUpdate) ajivaLayerRenderer!.Update(renderSystem);
                ToUpdate.Clear();
            }

            ajivaLayerRenderer!.UpdateSubmitInfoChecked();

            if (ChangingObserver.UpdateCycle(delta.Iteration)) UpdateGraphicsData();
            lock (CurrentGraphicsLayoutSwapLock)
            {
                DrawFrame();
            }
        }

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

        private void WindowResized()
        {
            RecreateCurrentGraphicsLayout();
        }

        public void DrawFrame()
        {
            var render = deviceSystem.GraphicsQueue!;
            var presentation = deviceSystem.PresentQueue!;

            deviceSystem.ExecuteSingleTimeCommands(QueueType.GraphicsQueue, CommandPoolSelector.Foreground);
            deviceSystem.ExecuteSingleTimeCommands(QueueType.GraphicsQueue, CommandPoolSelector.Background);
            deviceSystem.ExecuteSingleTimeCommands(QueueType.TransferQueue, CommandPoolSelector.Transit);

            lock (presentation)
            {
                lock (render)
                {
                    ajivaLayerRenderer!.DrawFrame(render, presentation);
                }
            }
        }

        public void ResolveDeps()
        {
            deviceSystem = Ecs.GetSystem<DeviceSystem>();
            windowSystem = Ecs.GetSystem<WindowSystem>();

            DepthFormat = (deviceSystem.PhysicalDevice ?? throw new InvalidOperationException()).FindDepthFormat();
        }

        protected void ReCreateRenderUnion()
        {
            ajivaLayerRenderer ??= new AjivaLayerRenderer(deviceSystem, windowSystem.Canvas);

            ajivaLayerRenderer.Init(Layers.Values.ToList());
        }

        public void UpdateGraphicsData()
        {
            ChangingObserver.Updated();
            ajivaLayerRenderer?.ForceFillBuffers();
        }

        public void AddUpdateLayer(IAjivaLayer layer)
        {
            layer.LayerChanged.OnChanged += LayerChangedOnOnChanged;
            Layers.Add(layer.PipelineLayer, layer);
        }

        private void LayerChangedOnOnChanged(IAjivaLayer sender)
        {
            if (!reInitAjivaLayerRendererNeeded) reInitAjivaLayerRendererNeeded = true;
            foreach (var layerLayerRenderComponentSystem in sender.LayerRenderComponentSystems)
            {
                layerLayerRenderComponentSystem.GraphicsDataChanged.OnChanged -= GraphicsDataChangedOnOnChanged;
                layerLayerRenderComponentSystem.GraphicsDataChanged.OnChanged += GraphicsDataChangedOnOnChanged;
            }
        }

        private void GraphicsDataChangedOnOnChanged(IAjivaLayerRenderSystem ajivaLayerRenderSystem)
        {
            ToUpdate.Add(ajivaLayerRenderSystem);
        }
    }
}
