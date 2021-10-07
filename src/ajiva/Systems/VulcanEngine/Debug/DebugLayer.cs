using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ajiva.Components;
using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Ecs.Component;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Utils;
using ajiva.Models;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Layers.Layer3d;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers;
using ajiva.Systems.VulcanEngine.Layers.Creation;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;
using ajiva.Utils.Changing;
using GlmSharp;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Debug
{
    [Dependent(typeof(WindowSystem))]
    public class DebugLayer : ComponentSystemBase<DebugComponent>, IInit, IUpdate, IAjivaLayerRenderSystem<UniformViewProj3d>
    {
        private MeshPool meshPool;
        private readonly object mainLock = new();
        public PipelineDescriptorInfos[] PipelineDescriptorInfos { get; set; }

        public IAChangeAwareBackupBufferOfT<DebugUniformModel> Models { get; set; }

        public Shader MainShader { get; set; }

        /// <inheritdoc />
        /// <inheritdoc />
        public DebugLayer(IAjivaEcs ecs) : base(ecs)
        {
        }
        
        /// <inheritdoc />
        public override DebugComponent RegisterComponent(IEntity entity, DebugComponent component)
        {
            if (!entity.TryGetComponent<Transform3d>(out var transform))
                throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

            transform.ChangingObserver.OnChanged += (_, after) =>
                Models.GetForChange((int)component.Id).Value.Model = after;

            Ecs.GetSystem<GraphicsSystem>().ChangingObserver.Changed();
            return base.RegisterComponent(entity, component);
        }

        /// <inheritdoc />
        public void Init()
        {
            var deviceSystem = Ecs.GetSystem<DeviceSystem>();

            MainShader = Shader.CreateShaderFrom(Ecs.GetSystem<AssetManager>(), "3d/debug", deviceSystem, "main");
            Models = new AChangeAwareBackupBufferOfT<DebugUniformModel>(1000000, deviceSystem);
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

        /// <inheritdoc />
        public void DrawComponents(RenderLayerGuard renderGuard)
        {
            meshPool.Reset();
            foreach (var (render, entity) in ComponentEntityMap)
            {
                if (!render.Render) continue;
                renderGuard.BindDescriptor(render.Id * (uint)Unsafe.SizeOf<DebugUniformModel>());
                meshPool.DrawMesh(renderGuard.Buffer, render.MeshId);
            }
        }

        /// <inheritdoc />
        public GraphicsPipelineLayer CreateGraphicsPipelineLayer(RenderPassLayer renderPassLayer)
        {
            return CreateDebugPipe.Default(renderPassLayer.Parent, renderPassLayer, Ecs.GetSystem<DeviceSystem>(), true, Vertex3D.GetBindingDescription(), Vertex3D.GetAttributeDescriptions(), MainShader, PipelineDescriptorInfos);
        }

        /// <inheritdoc />
        public IAjivaLayer<UniformViewProj3d> AjivaLayer { get; set; }
    }
    public struct DebugUniformModel : IComp<DebugUniformModel>
    {
        public mat4 Model;

        /// <inheritdoc />
        public bool CompareTo(DebugUniformModel other)
        {
            return other.Model == Model;
        }
    }

    public class DebugComponent : RenderMeshIdUnique<DebugComponent>
    {
        public bool DrawTransform { get; set; }
        public bool DrawWireframe { get; set; }
        public bool NoDepthTest { get; set; }
    }
}
