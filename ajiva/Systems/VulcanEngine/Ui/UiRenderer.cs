using System;
using System.Collections.Generic;
using ajiva.Components;
using ajiva.Ecs;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;
using ajiva.Helpers;
using ajiva.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Systems.VulcanEngine.Unions;
using GlmSharp;

namespace ajiva.Systems.VulcanEngine.Ui
{
    public class UiRenderer : ComponentSystemBase<ARenderAble2D>, IUpdate
    {
        /// <inheritdoc />
        protected override void Setup()
        {
            Ecs.RegisterUpdate(this);
        }

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
            ShaderSystem shaderSystem = Ecs.GetSystem<ShaderSystem>();

            var union = shaderSystem.ShaderUnions[PipelineName.PipeLine2d];

            union.ViewProj.UpdateExpresion((int index, ref UniformViewProj value) =>
            {
                value.View = mat4.Identity;
            });
            union.ViewProj.Copy();

            var ids = new List<uint>();
            foreach (var entity in ComponentEntityMap.Keys)
            {
                ids.Add(entity.Id);
                union.UniformModels.UpdateOne(new() {Model = mat4.Translate((float)r.NextDouble(),(float)r.NextDouble(),0)}, entity.Id);
            }
            
            union.UniformModels.CopyRegions(ids);
        }
    }
}
