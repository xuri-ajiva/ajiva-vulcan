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
        public override RenderMesh2D RegisterComponent(IEntity entity, RenderMesh2D component)
        {
            Ecs.GetSystem<GraphicsSystem>().ChangingObserver.Changed(); //todo some changes on a single layer
            
            if (!entity.TryGetComponent<Transform2d>(out var transform))
                throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

            transform.ChangingObserver.OnChanged += (_, model) => Models.GetForChange((int)component.Id).Value.Model = model;

            return base.RegisterComponent(entity, component);
        }                                                                           

        /// <inheritdoc />
        public Mesh2dRenderLayer(IAjivaEcs ecs) : base(ecs)
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
            var res = GraphicsPipelineLayerCreator.Default(renderPassLayer.Parent, renderPassLayer, Ecs.GetSystem<DeviceSystem>(), true, Vertex2D.GetBindingDescription(), Vertex2D.GetAttributeDescriptions(), MainShader, PipelineDescriptorInfos);

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

            MainShader = Shader.CreateShaderFrom(Ecs.GetSystem<AssetManager>(), "2d", deviceSystem, "main");
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
            lock (mainLock)
                Models.CommitChanges();
        }
    }
}
