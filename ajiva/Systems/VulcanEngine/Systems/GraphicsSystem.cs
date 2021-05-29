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
    [Dependent(typeof(TextureSystem))]
    public class GraphicsSystem : SystemBase, IInit, IUpdate
    {
        public IChangingObserver ChangingObserver { get; } = new ChangingObserver(100);

        private static readonly object CurrentGraphicsLayoutSwapLock = new();

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
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
        private LayerSystem layerSystem;
        private IRenderMeshPool meshPool;

        public void ResolveDeps()
        {
            deviceSystem = Ecs.GetSystem<DeviceSystem>();
            imageSystem = Ecs.GetComponentSystem<ImageSystem, AImage>();
            windowSystem = Ecs.GetSystem<WindowSystem>();
            layerSystem = Ecs.GetSystem<LayerSystem>();
            meshPool = Ecs.GetInstance<MeshPool>();

            render = deviceSystem.GraphicsQueue!;
            presentation = deviceSystem.PresentQueue!;
            DepthFormat = (deviceSystem.PhysicalDevice ?? throw new InvalidOperationException()).FindDepthFormat();
        }

        private const int DISPOSE_DALEY = 1000;

        protected void ReCreateRenderUnion()
        {
            renderUnion?.DisposeIn(DISPOSE_DALEY);

            renderUnion = RenderUnion.CreateRenderUnion(deviceSystem, windowSystem.Canvas);

            foreach (var (_, layer) in layerSystem.Layers)
            {
                layer.ReCreateDepthImage(imageSystem, DepthFormat, windowSystem.Canvas);
                renderUnion.AddUpdateLayer(layer, deviceSystem);
            }

            UpdateGraphicsData();
        }

        private void UpdateGraphicsData()
        {
            LogHelper.Log("Updating BufferData");
            ChangingObserver.Updated();

            foreach (var (_, layer) in layerSystem.Layers)
            {
                renderUnion.FillFrameBuffer(layer.PipelineLayer, layer.GetRenders(), meshPool);
            }
        }

  #endregion
    }
}
