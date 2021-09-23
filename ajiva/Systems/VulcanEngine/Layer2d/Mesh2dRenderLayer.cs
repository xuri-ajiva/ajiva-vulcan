using System.Runtime.CompilerServices;
using ajiva.Components;
using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Utils;
using ajiva.Models;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Layers.Layer2d;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layer3d;
using ajiva.Systems.VulcanEngine.Layers;
using ajiva.Systems.VulcanEngine.Layers.Creation;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;
using ajiva.Utils.Changing;

namespace ajiva.Systems.VulcanEngine.Layer2d
{
    [Dependent(typeof(Ajiva3dLayerSystem))]
    public class Mesh2dRenderLayer : ComponentSystemBase<RenderMesh2D>, IInit, IUpdate, IAjivaLayerRenderSystem<UniformLayer2d>
    {
        private MeshPool meshPool;
        private readonly object mainLock = new();

        public IAChangeAwareBackupBufferOfT<SolidUniformModel2d> Models { get; set; }

        /// <inheritdoc />
        public override RenderMesh2D CreateComponent(IEntity entity)
        {
            Ecs.GetSystem<GraphicsSystem>().ChangingObserver.Changed(); //todo some changes on a single layer
            var rnd = new RenderMesh2D();
            ComponentEntityMap.Add(rnd, entity);
            return rnd;
        }

        /// <inheritdoc />
        public override void AttachNewComponent(IEntity entity)
        {
            entity.AddComponent(CreateComponent(entity));
        }

        /// <inheritdoc />
        public Mesh2dRenderLayer(AjivaEcs ecs) : base(ecs)
        {
        }

        public PipelineDescriptorInfos[] PipelineDescriptorInfos { get; set; }

        public Shader MainShader { get; set; }
        public IAjivaLayer<UniformLayer2d> AjivaLayer { get; set; }

        /// <inheritdoc />
        public void DrawComponents(RenderLayerGuard renderGuard)
        {
            meshPool.Reset();
            foreach (var (render, entity) in ComponentEntityMap)
            {
                if (!render.Render) continue;
                renderGuard.BindDescriptor(render.Id * (uint)Unsafe.SizeOf<SolidUniformModel2d>());
                meshPool.DrawMesh(renderGuard.Buffer, render.MeshId);
            }
        }

        /// <inheritdoc />
        public GraphicsPipelineLayer CreateGraphicsPipelineLayer(RenderPassLayer renderPassLayer)
        {
            return GraphicsPipelineLayerCreator.Default(renderPassLayer.Parent, renderPassLayer, Ecs.GetSystem<DeviceSystem>(), true, Vertex2D.GetBindingDescription(), Vertex2D.GetAttributeDescriptions(), MainShader, PipelineDescriptorInfos);
        }

        /// <inheritdoc />
        public void Init(AjivaEcs ecs)
        {
            var deviceSystem = Ecs.GetSystem<DeviceSystem>();

            MainShader = Shader.CreateShaderFrom("./Shaders/2d/", deviceSystem, "main");
            Models = new AChangeAwareBackupBufferOfT<SolidUniformModel2d>(1000000, deviceSystem);
            meshPool = Ecs.GetInstance<MeshPool>();

            PipelineDescriptorInfos = Layers.PipelineDescriptorInfos.CreateFrom(
                AjivaLayer.LayerUniform.Uniform.Buffer!, (uint)AjivaLayer.LayerUniform.SizeOfT,
                Models.Uniform.Buffer!, (uint)Models.SizeOfT,
                Ecs.GetComponentSystem<TextureSystem, ATexture>().TextureSamplerImageViews
            );
        }

        /// <inheritdoc />
        public void Update(UpdateInfo delta)
        {
            foreach (var (renderAble, entity) in ComponentEntityMap)
            {
                if (entity.TryGetComponent(out Transform2d? transform) && transform!.ChangingObserver.UpdateCycle(delta.Iteration))
                {
                    Models.GetForChange((int)renderAble.Id).Value.Model = transform.ModelMat;
                    transform.ChangingObserver.Updated();
                }
                /*if (renderAble.ChangingObserver.UpdateCycle(delta.Iteration) /*entity.TryGetComponent(out texture) && texture!.Dirty ||#1#)
                {
                    renderAble.ChangingObserver.Updated();
                }*/
            }

            lock (mainLock)
                Models.CommitChanges();
        }
    }
}
