using System.Collections.Generic;
using System.Linq;
using ajiva.Components;
using ajiva.Ecs;
using ajiva.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Systems.VulcanEngine.Ui;
using ajiva.Systems.VulcanEngine.Unions;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.EngineManagers
{
    public class GraphicsLayout : ThreadSaveCreatable
    {
        private readonly AjivaEcs ecs;
        private AImage? DepthImage { get; set; }

        private Format? DepthFormat { get; set; }

        public GraphicsLayout(AjivaEcs ecs)
        {
            this.ecs = ecs;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            DepthImage?.Dispose();
            DepthImage = null;

            renderUnion?.Dispose();
            renderUnion = null!;
        }
        
        private Queue render;
        private Queue presentation;

        private static int i = 0;

        /// <inheritdoc />
        protected override void Create()
        {
            var ds = ecs.GetSystem<DeviceSystem>();
            var im = ecs.GetComponentSystem<ImageSystem, AImage>();
            var wi = ecs.GetSystem<WindowSystem>();
            var sh = ecs.GetSystem<ShaderSystem>();
            var tx = ecs.GetComponentSystem<TextureSystem, ATexture>();
            var ar = ecs.GetComponentSystem<AjivaRenderEngine, ARenderAble3D>();
            var ui = ecs.GetComponentSystem<UiRenderer, ARenderAble2D>();

            render = ds.GraphicsQueue!;
            presentation = ds.PresentQueue!;

            CreateDepthImage(im, ds.PhysicalDevice!);

            renderUnion = RenderUnion.CreateRenderUnion(ds.PhysicalDevice, ds.Device!, wi.Surface!, wi.SurfaceExtent, sh, tx.TextureSamplerImageViews, true, DepthImage!, ds.CommandPool);

            //renderUnion.FillFrameBuffers(ar.ComponentEntityMap.Keys.Union<ARenderAble>(ui.ComponentEntityMap.Keys));
            renderUnion.FillFrameBuffers(new Dictionary<PipelineName, List<ARenderAble>>()
            {
                [PipelineName.PipeLine2d] = ui.ComponentEntityMap.Keys.Cast<ARenderAble>().ToList(),
                [PipelineName.PipeLine3d] = ar.ComponentEntityMap.Keys.Cast<ARenderAble>().ToList(),
            });
        }

        private RenderUnion renderUnion;

        private void CreateDepthImage(ImageSystem imageSystem, PhysicalDevice device)
        {
            DepthFormat = device.FindDepthFormat();

            DepthImage ??= imageSystem.CreateManagedImage(DepthFormat.Value, ImageAspectFlags.Depth, ecs.GetSystem<WindowSystem>().SurfaceExtent);
        }

        public void DrawFrame()
        {
            renderUnion.DrawFrame(render, presentation);
        }
    }
}
