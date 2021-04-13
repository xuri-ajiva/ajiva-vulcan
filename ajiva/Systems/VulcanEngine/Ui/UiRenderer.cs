using System.Collections.Generic;
using ajiva.Components.RenderAble;
using ajiva.Ecs;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Utils;
using ajiva.Models.Buffer;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Systems.VulcanEngine.Unions;
using ajiva.Utils;
using GlmSharp;

namespace ajiva.Systems.VulcanEngine.Ui
{
    [Dependent(typeof(ShaderSystem), typeof(WindowSystem))]
    public class UiRenderer : ComponentSystemBase<ARenderAble2D>, IUpdate, IInit
    {

        /// <inheritdoc />
        public override void AttachNewComponent(IEntity entity)
        {
            entity.AddComponent(CreateComponent(entity));
        }

        /// <inheritdoc />
        public override ARenderAble2D CreateComponent(IEntity entity)
        {
            var comp = new ARenderAble2D();
            ComponentEntityMap.Add(comp, entity);
            return comp;
        }

        private Random r = new();

        /// <inheritdoc />
        public void Update(UpdateInfo delta)
        {
            var union = Ecs.GetSystem<ShaderSystem>().ShaderUnions[PipelineName.PipeLine2d];

            var ids = new List<uint>();
            foreach (var entity in ComponentEntityMap.Keys)
            {
                ids.Add(entity.Id);
                union.UniformModels.UpdateOne(new() {Model = mat4.Scale(.1f)}, entity.Id);
            }

            union.UniformModels.CopyRegions(ids);
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
        }

        /// <inheritdoc />
        public UiRenderer(AjivaEcs ecs) : base(ecs)
        {
        }
    }
}
