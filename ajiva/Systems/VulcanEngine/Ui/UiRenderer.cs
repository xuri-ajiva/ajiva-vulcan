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
            Ecs.GetSystem<GraphicsSystem>().ChangingObserver.Changed();
            var comp = new ARenderAble2D();
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
        }

        private ShaderUnion? union;
        private WindowSystem window;

        private void MouseMoved(vec2 pos)
        {
            var posNew = new vec2(pos.x / window.Canvas.WidthF, pos.y / window.Canvas.HeightF);
            //LogHelper.Log(posNew);
            if (ComponentEntityMap.Count > 0)
            {
                var ids = new List<uint>();
                foreach (var entity in ComponentEntityMap.Keys)
                {
                    ids.Add(entity.Id);
                    union.UniformModels.UpdateExpresionOne(entity.Id, (uint index, ref UniformModel value) =>
                    {
                        value.Model = mat4.Translate(posNew.x, posNew.y, 0) * mat4.Scale(.1f);
                        return true;
                    });
                    union.UniformModels.UpdateOne(new() {Model = mat4.Translate(posNew.x, posNew.y, 0) * mat4.Scale(.1f)}, entity.Id);
                }

                union.UniformModels.CopyRegions(ids);

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
    }
}
