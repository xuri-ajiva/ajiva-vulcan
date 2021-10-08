using System;
using ajiva.Components.Media;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Utils;
using ajiva.Utils;
using GlmSharp;
using SharpVk.Interop;

namespace ajiva.Systems
{
    public class TransformComponentSystem : ComponentSystemBase<Transform3d>, IUpdate
    {
        public TransformComponentSystem(IAjivaEcs ecs) : base(ecs)
        {
        }

        private Random r = new Random();

        /// <inheritdoc />
        public void Update(UpdateInfo delta)
        {
            /*var rSeed2 = r.Next(1000,5000);
            var pod = (float)(r.NextDouble() - .5)*2;
            foreach (var (key, val) in ComponentEntityMap)
            {
                if (val.Id % rSeed2 < 100)
                {
                    key.RefRotation(((ref vec3 vec) => vec.x += .5f));
                    key.RefRotation(((ref vec3 vec) => vec.x += pod));
                }
            }*/
        }
    }
    public class Transform2dComponentSystem : ComponentSystemBase<Transform2d>
    {
        public Transform2dComponentSystem(IAjivaEcs ecs) : base(ecs)
        {
        }
    }
}
