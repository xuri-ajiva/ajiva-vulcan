using System;
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
using ajiva.Models.Layers.Layer3d;
using ajiva.Systems.Assets;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers;
using ajiva.Systems.VulcanEngine.Layers.Creation;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;

namespace ajiva.Systems.VulcanEngine.Layer3d
{
    [Dependent(typeof(Ajiva3dLayerSystem))]
    public class SolidMeshRenderLayer : ComponentSystemBase<RenderMesh3D>, IInit, IUpdate, IAjivaLayerRenderSystem<UniformViewProj3d>
    {
        private MeshPool meshPool;
        private readonly object mainLock = new();

        public IAChangeAwareBackupBufferOfT<SolidUniformModel> Models { get; set; }

        /// <inheritdoc />
        public override RenderMesh3D RegisterComponent(IEntity entity, RenderMesh3D component)
        {
            if (!entity.TryGetComponent<Transform3d>(out var transform))
                throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

            component.Models = Models;
            transform.ChangingObserver.OnChanged += component.OnTransformChange;
            //if (entity.TryGetComponent<TextureComponent>(out var texture))
            //    texture.ChangingObserver.OnChanged += _ => Models.GetForChange((int)component.Id).Value.fragtexSamplerId = texture.TextureId;

            //component.ChangingObserver.OnChanged += _ => Models.GetForChange((int)component.Id).Value.TextureSamplerId = component.Id;

            Ecs.GetSystem<GraphicsSystem>().ChangingObserver.Changed();
            return base.RegisterComponent(entity, component);
        }

        /// <inheritdoc />
        public SolidMeshRenderLayer(IAjivaEcs ecs) : base(ecs)
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
            var res = GraphicsPipelineLayerCreator.Default(renderPassLayer.Parent, renderPassLayer, Ecs.GetSystem<DeviceSystem>(), true, Vertex3D.GetBindingDescription(), Vertex3D.GetAttributeDescriptions(), MainShader, PipelineDescriptorInfos);

            foreach (var entity in ComponentEntityMap)
            {
                entity.Key.ChangingObserver.Changed();
            }

            return res;
        }

        /// <inheritdoc />
        public void Init()
        {
            var deviceSystem = Ecs.GetSystem<DeviceSystem>();

            MainShader = Shader.CreateShaderFrom(Ecs.GetSystem<AssetManager>(), "3d/Solid", deviceSystem, "main");
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
            lock (mainLock)
                Models.CommitChanges();
        }
    }
}
