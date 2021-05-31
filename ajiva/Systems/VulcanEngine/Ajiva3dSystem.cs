using System.Collections.Generic;
using System.Linq;
using ajiva.Components;
using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Ecs;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Utils;
using ajiva.Entities;
using ajiva.Models;
using ajiva.Models.Buffer;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Systems.VulcanEngine.Unions;
using ajiva.Utils;
using ajiva.Utils.Changing;
using SharpVk;
using SharpVk.Glfw;

namespace ajiva.Systems.VulcanEngine
{
    [Dependent(typeof(WindowSystem))]
    public class Ajiva3dSystem : ComponentSystemBase<RenderMesh3D>, IInit, IUpdate, IAjivaLayer
    {
        public Cameras.Camera MainCamara
        {
            get => mainCamara!;
            set
            {
                mainCamara?.Dispose();
                mainCamara = value;
            }
        }

        private Cameras.Camera? mainCamara;

        private void UpdateCamaraProjView()
        {
            ref var value = ref ViewProj.GetRef(0);
            var changed = false;
            if (value.View != mainCamara!.View)
            {
                changed = true;
                value.View = MainCamara.View;
            }
            if (value.Proj != MainCamara.Projection) //todo: we flip the [1,1] value sow it is never the same
            {
                value.Proj = MainCamara.Projection;
                value.Proj[1, 1] *= -1;

                changed = true;
            }
            if (changed)
                    ViewProj.Commit((0));
        }

        /// <inheritdoc />
        public void Update(UpdateInfo delta)
        {
            var updated = new List<uint>();
            foreach (var (renderAble, entity) in ComponentEntityMap)
            {
                Transform3d? transform = null!;
                if (entity.TryGetComponent(out transform) && transform!.ChangingObserver.UpdateCycle(delta.Iteration))
                {
                        
                    Models.Value[(int)renderAble.Id].Model = transform.ModelMat;
                    Models.SetChanged((int)renderAble.Id, true);
                    transform.ChangingObserver.Updated();
                }
                if (renderAble.ChangingObserver.UpdateCycle(delta.Iteration) /*entity.TryGetComponent(out texture) && texture!.Dirty ||*/)
                {
                    Models.Value[(int)renderAble.Id].TextureSamplerId = renderAble!.Id; // texture?.TextureId ?? 0
                    Models.SetChanged((int)renderAble.Id, true);
                    renderAble.ChangingObserver.Updated();
                }
            }

            lock (MainLock)
                Models.CommitChanges();

            lock (MainLock)
                UpdateCamaraProjView();
        }

        /// <inheritdoc />
        public void Init(AjivaEcs ecs)
        {
            var window = Ecs.GetSystem<WindowSystem>();

            window.OnResize += () =>
            {
                lock (MainLock)
                {
                    foreach (var entity in ComponentEntityMap)
                    {
                        entity.Key.ChangingObserver.Changed();
                    }

                    mainCamara?.UpdatePerspective(mainCamara.Fov, window.Canvas.WidthF, window.Canvas.HeightF);
                }
            };

            window.OnKeyEvent += delegate(object? _, Key key, int scancode, InputAction action, Modifier modifier)
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
            };

            window.OnMouseMove += delegate(object? _, AjivaMouseMotionCallbackEventArgs e)
            {
                if (e.ActiveLayer == AjivaEngineLayer.Layer3d)
                    MainCamara.OnMouseMoved(e.Delta.x, e.Delta.y);
            };

            var deviceSystem = Ecs.GetSystem<DeviceSystem>();

            Canvas = window.Canvas;

            MainShader = Shader.CreateShaderFrom("./Shaders/3d", deviceSystem, "main");
            Models = new AChangeAwareBackupBufferOfT<UniformModel>(25000, deviceSystem);
            ViewProj = new AChangeAwareBackupBufferOfT<UniformViewProj>(1, deviceSystem);

            PipelineDescriptorInfos = ajiva.Systems.VulcanEngine.Unions.PipelineDescriptorInfos.CreateFrom(
                ViewProj.Uniform.Buffer!, (uint)ViewProj.SizeOfT,
                Models.Uniform.Buffer!, (uint)Models.SizeOfT,
                Ecs.GetComponentSystem<TextureSystem, ATexture>().TextureSamplerImageViews
            );
        }

        public IAChangeAwareBackupBufferOfT<UniformViewProj> ViewProj { get; set; }
        public IAChangeAwareBackupBufferOfT<UniformModel> Models { get; set; }

        /// <inheritdoc />
        public override RenderMesh3D CreateComponent(IEntity entity)
        {
            Ecs.GetSystem<GraphicsSystem>().ChangingObserver.Changed();
            var rnd = new RenderMesh3D();
            ComponentEntityMap.Add(rnd, entity);
            return rnd;
        }

        /// <inheritdoc />
        public override void AttachNewComponent(IEntity entity)
        {
            entity.AddComponent(CreateComponent(entity));
        }

        private object MainLock { get; } = new();

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            lock (MainLock)
            {
                Ecs.GetSystem<DeviceSystem>().WaitIdle();
                mainCamara?.Dispose();
            }
            base.ReleaseUnmanagedResources(disposing);
        }

        /// <inheritdoc />
        public Ajiva3dSystem(AjivaEcs ecs) : base(ecs)
        {
        }

#region Layer

        /// <inheritdoc />
        public AjivaVulkanPipeline PipelineLayer { get; } = AjivaVulkanPipeline.Pipeline3d;

        /// <inheritdoc />
        public AImage DepthImage { get; set; }

        /// <inheritdoc />
        public Shader MainShader { get; set; }

        /// <inheritdoc />
        public PipelineDescriptorInfos[] PipelineDescriptorInfos { get; set; }

        /// <inheritdoc />
        public bool DepthEnabled { get; set; } = true;

        /// <inheritdoc />
        public Canvas Canvas { get; set; }

        /// <inheritdoc />
        public VertexInputBindingDescription VertexInputBindingDescription { get; } = Vertex3D.GetBindingDescription();

        /// <inheritdoc />
        public VertexInputAttributeDescription[] VertexInputAttributeDescriptions { get; } = Vertex3D.GetAttributeDescriptions();

        /// <inheritdoc />
        public ClearValue[] ClearValues { get; set; } =
        {
            new ClearColorValue(.1f, .1f, .1f, .1f),
            new ClearDepthStencilValue(1, 0),
        };

        /// <inheritdoc />
        public List<IRenderMesh> GetRenders()
        {
            return ComponentEntityMap.Keys.Cast<IRenderMesh>().ToList();
        }

  #endregion
    }
}
