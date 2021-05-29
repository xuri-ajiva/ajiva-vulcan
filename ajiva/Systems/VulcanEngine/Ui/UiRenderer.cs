using System;
using System.Collections.Generic;
using System.Linq;
using ajiva.Components;
using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Ecs;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Utils;
using ajiva.Models;
using ajiva.Models.Buffer;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Systems.VulcanEngine.Unions;
using ajiva.Utils;
using GlmSharp;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Ui
{
    [Dependent(typeof(ShaderSystem), typeof(WindowSystem))]
    public class UiRenderer : ComponentSystemBase<RenderMesh2D>, IUpdate, IInit, IAjivaLayer
    {
        /// <inheritdoc />
        public override void AttachNewComponent(IEntity entity)
        {
            entity.AddComponent(CreateComponent(entity));
        }

        /// <inheritdoc />
        public override RenderMesh2D CreateComponent(IEntity entity)
        {
            Ecs.GetSystem<GraphicsSystem>().ChangingObserver.Changed();
            var comp = new RenderMesh2D();
            ComponentEntityMap.Add(comp, entity);
            return comp;
        }

        /// <inheritdoc />
        public void Update(UpdateInfo delta)
        {
            /*var ids = new List<uint>();
            foreach (var entity in ComponentEntityMap.Keys)
            {
                ids.Add(entity.Id);
                union.UniformModels.UpdateOne(new() {Model = mat4.Scale(.1f)}, entity.Id);
            }
    
            union.UniformModels.CopyRegions(ids);      */

            lock (updatedIds)
            {
                if (updatedIds.Count != 0)
                {
                    union.UniformModels.CopyRegions(updatedIds);
                    updatedIds.Clear();
                }
            }
        }

        /// <inheritdoc />
        public void Init(AjivaEcs ecs)
        {
            union = Ecs.GetSystem<ShaderSystem>().ShaderUnions[AjivaEngineLayer.Layer2d];
            union.ViewProj.UpdateExpresion((uint index, ref UniformViewProj value) =>
            {
                value.View = mat4.Translate(-1, -1, 0) * mat4.Scale(2);
                return true;
            });
            union.ViewProj.Copy();

            union.UniformModels.UpdateExpresion((uint index, ref UniformModel value) =>
            {
                value.Model = mat4.Translate(.5f, .5f, 0) * mat4.Scale(0.1f);
                return true;
            });
            union.UniformModels.Copy();

            window = Ecs.GetSystem<WindowSystem>();

            window.OnMouseMove += delegate(object? sender, AjivaMouseMotionCallbackEventArgs args)
            {
                if (args.ActiveLayer == AjivaEngineLayer.Layer2d)
                {
                    MouseMoved(args.Pos);
                }
            };

            PipelineDescriptorInfos = ajiva.Systems.VulcanEngine.Unions.PipelineDescriptorInfos.CreateFrom(
                Ecs.GetSystem<ShaderSystem>().ShaderUnions[AjivaEngineLayer.Layer2d].ViewProj,
                Ecs.GetSystem<ShaderSystem>().ShaderUnions[AjivaEngineLayer.Layer2d].UniformModels,
                Ecs.GetComponentSystem<TextureSystem, ATexture>().TextureSamplerImageViews
            );
            Canvas = window.Canvas;
        }

        private ShaderUnion? union;
        private WindowSystem window;

        public List<uint> updatedIds = new();

        private void MouseMoved(vec2 pos)
        {
            var posNew = new vec2(pos.x / window.Canvas.WidthF, pos.y / window.Canvas.HeightF);
            //LogHelper.Log(posNew);
            if (ComponentEntityMap.Count > 0)
            {
                lock (updatedIds)
                    foreach (var entity in ComponentEntityMap.Keys)
                    {
                        updatedIds.Add(entity.Id);
                        union.UniformModels.UpdateExpresionOne(entity.Id, (uint index, ref UniformModel value) =>
                        {
                            value.Model = mat4.Translate(posNew.x, posNew.y, 0) * mat4.Scale(.1f);
                            return true;
                        });
                        union.UniformModels.UpdateOne(new() {Model = mat4.Translate(posNew.x, posNew.y, 0) * mat4.Scale(.1f)}, entity.Id);
                    }

                /*var cmp = ComponentEntityMap.Keys.First();

                union.UniformModels.UpdateExpresionOne(cmp.Id, delegate(uint index, ref UniformModel value)
                {
                    value.Model.m30 = pos.x / window.Width;
                    value.Model.m31 = pos.y / window.Height;
                    //value.Model.m32 = z;
                    return true;
                });
                union.UniformModels.Copy();*/
            }
        }

        /// <inheritdoc />
        public UiRenderer(AjivaEcs ecs) : base(ecs)
        {
        }

#region Layer

        /// <inheritdoc />
        public AjivaVulkanPipeline PipelineLayer { get; } = AjivaVulkanPipeline.Pipeline2d;

        /// <inheritdoc />
        public AImage DepthImage { get; set; }

        /// <inheritdoc />
        public Shader MainShader
        {
            get => Ecs.GetSystem<ShaderSystem>().ShaderUnions[AjivaEngineLayer.Layer2d].Main;
            set => throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public PipelineDescriptorInfos[] PipelineDescriptorInfos { get; set; }

        /// <inheritdoc />
        public bool DepthEnabled { get; set; } = false;

        /// <inheritdoc />
        public Canvas Canvas { get; set; }

        /// <inheritdoc />
        public VertexInputBindingDescription VertexInputBindingDescription { get; } = Vertex2D.GetBindingDescription();

        /// <inheritdoc />
        public VertexInputAttributeDescription[] VertexInputAttributeDescriptions { get; } = Vertex2D.GetAttributeDescriptions();

        /// <inheritdoc />
        public ClearValue[] ClearValues { get; set; } = Array.Empty<ClearValue>();

        /// <inheritdoc />
        public List<IRenderMesh> GetRenders()
        {
            return ComponentEntityMap.Keys.Cast<IRenderMesh>().ToList();
        }

  #endregion
    }
}
