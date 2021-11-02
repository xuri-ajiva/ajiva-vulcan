#region

using System;
using System.Collections.Generic;
using System.Linq;
using ajiva.Components.Media;
using ajiva.Ecs;
using ajiva.Ecs.System;
using ajiva.Ecs.Utils;
using ajiva.Entities;
using ajiva.Models.Buffer;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Layers.Layer3d;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers.Creation;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;
using ajiva.Utils.Changing;
using Ajiva.Wrapper.Logger;
using SharpVk;
using SharpVk.Glfw;

#endregion

namespace ajiva.Systems.VulcanEngine.Layer3d
{
    [Dependent(typeof(WindowSystem), typeof(GraphicsSystem))]
    public class Ajiva3dLayerSystem : SystemBase, IInit, IUpdate, IAjivaLayer<UniformViewProj3d>
    {
        private Cameras.Camera? mainCamara;
        private WindowSystem window;

        /// <inheritdoc />
        public Ajiva3dLayerSystem(IAjivaEcs ecs) : base(ecs)
        {
            LayerChanged = new ChangingObserver<IAjivaLayer>(this);
        }

        public Cameras.Camera MainCamara
        {
            get => mainCamara!;
            set
            {
                mainCamara?.Dispose();
                mainCamara = value;
            }
        }

        private object MainLock { get; } = new object();

        public IAChangeAwareBackupBufferOfT<UniformViewProj3d> LayerUniform { get; set; }

        /// <inheritdoc />
        public IChangingObserver<IAjivaLayer> LayerChanged { get; }

        /// <inheritdoc />
        List<IAjivaLayerRenderSystem> IAjivaLayer.LayerRenderComponentSystems => new List<IAjivaLayerRenderSystem>(LayerRenderComponentSystems);

        /// <inheritdoc />
        public AjivaVulkanPipeline PipelineLayer { get; } = AjivaVulkanPipeline.Pipeline3d;

        public ClearValue[] ClearValues { get; } =
        {
            new ClearColorValue(.1f, .1f, .1f, .1f),
            new ClearDepthStencilValue(1.0f, 0),
        };

        /// <inheritdoc />
        public List<IAjivaLayerRenderSystem<UniformViewProj3d>> LayerRenderComponentSystems { get; } = new List<IAjivaLayerRenderSystem<UniformViewProj3d>>();

        /// <inheritdoc />
        public RenderPassLayer CreateRenderPassLayer(SwapChainLayer swapChainLayer)
        {
            return RenderPassLayerCreator.Default(swapChainLayer, Ecs.GetSystem<DeviceSystem>(), Ecs.GetComponentSystem<ImageSystem, AImage>());
        }

        /// <inheritdoc />
        public void Init()
        {
            window = Ecs.GetSystem<WindowSystem>();

            window.OnResize += OnWindowResize;

            window.OnKeyEvent += OnWindowKeyEvent;

            window.OnMouseMove += OnWindowMouseMove;

            var deviceSystem = Ecs.GetSystem<DeviceSystem>();

            LayerUniform = new AChangeAwareBackupBufferOfT<UniformViewProj3d>(1, deviceSystem);

            if (Ecs.TryCreateEntity<Cameras.FpsCamera>(out var mCamTmp))
            {
                MainCamara = mCamTmp;
            }
            else
            {
                ALog.Error("cam not created");
            }
            MainCamara.UpdatePerspective(90, window.Canvas.Width, window.Canvas.Height);
            MainCamara.MovementSpeed = .01f;
        }

        /// <inheritdoc />
        public void Update(UpdateInfo delta)
        {
            lock (MainLock)
            {
                UpdateCamaraProjView();
            }
        }

        private void UpdateCamaraProjView()
        {
            var byRef = LayerUniform.GetForChange(0);
            var changed = false;
            if (byRef.Value.View != mainCamara!.View)
            {
                changed = true;
                byRef.Value.View = MainCamara.View;
            }
            if (byRef.Value.Proj != MainCamara.Projection) //todo: we flip the [1,1] value sow it is never the same
            {
                byRef.Value.Proj = MainCamara.Projection;
                byRef.Value.Proj[1, 1] *= -1;

                changed = true;
            }
            if (changed)
                LayerUniform.Commit(0);
        }

        private void OnWindowMouseMove(object? _, AjivaMouseMotionCallbackEventArgs e)
        {
            lock (MainLock)
            {
                var (_, delta, ajivaEngineLayer) = e;
                if (ajivaEngineLayer == AjivaEngineLayer.Layer3d) MainCamara.OnMouseMoved(delta.x, delta.y);
            }
        }

        private void OnWindowKeyEvent(object? _, Key key, int scancode, InputAction action, Modifier modifier)
        {
            lock (MainLock)
            {
                var down = action != InputAction.Release;

                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (key)
                {
                    case Key.W:
                        MainCamara.Keys.up = down;
                        break;
                    case Key.D:
                        MainCamara.Keys.right = down;
                        break;
                    case Key.S:
                        MainCamara.Keys.down = down;
                        break;
                    case Key.A:
                        MainCamara.Keys.left = down;
                        break;
                }
            }
        }

        private void OnWindowResize()
        {
            lock (MainLock)
            {
                mainCamara?.UpdatePerspective(mainCamara.Fov, window.Canvas.WidthF, window.Canvas.HeightF);
            }
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            lock (MainLock)
            {
                foreach (var renderSystem in LayerRenderComponentSystems) renderSystem.Dispose();
                LayerUniform.Dispose();
                Ecs.GetSystem<DeviceSystem>().WaitIdle();
                mainCamara?.Dispose();
            }
            base.ReleaseUnmanagedResources(disposing);
        }
    }
}
