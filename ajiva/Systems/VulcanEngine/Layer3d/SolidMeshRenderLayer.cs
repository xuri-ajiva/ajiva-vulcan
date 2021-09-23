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
using ajiva.Models.Buffer;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Layers.Layer3d;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers;
using ajiva.Systems.VulcanEngine.Layers.Creation;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;
using ajiva.Utils.Changing;

namespace ajiva.Systems.VulcanEngine.Layer3d
{
    [Dependent(typeof(Ajiva3dLayerSystem))]
    public class SolidMeshRenderLayer : ComponentSystemBase<RenderMesh3D>, IInit, IUpdate, IAjivaLayerRenderSystem<UniformViewProj3d>
    {
        private MeshPool meshPool;
        private readonly object mainLock = new();

        public IAChangeAwareBackupBufferOfT<SolidUniformModel> Models { get; set; }

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

        /// <inheritdoc />
        public SolidMeshRenderLayer(AjivaEcs ecs) : base(ecs)
        {
        }

        public PipelineDescriptorInfos[] PipelineDescriptorInfos { get; set; }

        public Shader MainShader { get; set; }
        public IAjivaLayer<UniformViewProj3d> AjivaLayer { get; set; }

        /// <inheritdoc />
        public void DrawComponents(RenderLayerGuard renderGuard)
        {
            meshPool.Reset();
            foreach (var (render, entity) in ComponentEntityMap)
            {
                if (!render.Render) continue;
                renderGuard.BindDescriptor(render.Id * (uint)Unsafe.SizeOf<SolidUniformModel>());
                meshPool.DrawMesh(renderGuard.Buffer, render.MeshId);
            }
        }

        /// <inheritdoc />
        public GraphicsPipelineLayer CreateGraphicsPipelineLayer(RenderPassLayer renderPassLayer)
        {
            return GraphicsPipelineLayerCreator.Default(renderPassLayer.Parent, renderPassLayer, Ecs.GetSystem<DeviceSystem>(), true, Vertex3D.GetBindingDescription(), Vertex3D.GetAttributeDescriptions(), MainShader, PipelineDescriptorInfos);
        }

        /// <inheritdoc />
        public void Init(AjivaEcs ecs)
        {
            var deviceSystem = Ecs.GetSystem<DeviceSystem>();

            MainShader = Shader.CreateShaderFrom("./Shaders/3d/Soild/", deviceSystem, "main");
            Models = new AChangeAwareBackupBufferOfT<SolidUniformModel>(1000000, deviceSystem);
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
                if (entity.TryGetComponent(out Transform3d? transform) && transform!.ChangingObserver.UpdateCycle(delta.Iteration))
                {
                    Models.GetForChange((int)renderAble.Id).Value.Model = transform.ModelMat;
                    transform.ChangingObserver.Updated();
                }
                if (renderAble.ChangingObserver.UpdateCycle(delta.Iteration) /*entity.TryGetComponent(out texture) && texture!.Dirty ||*/)
                {
                    Models.GetForChange((int)renderAble.Id).Value.TextureSamplerId = renderAble.Id;
                    renderAble.ChangingObserver.Updated();
                }
            }

            lock (mainLock)
                Models.CommitChanges();
        }
    }
}
