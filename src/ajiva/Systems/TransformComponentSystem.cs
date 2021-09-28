using ajiva.Components.Media;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;

namespace ajiva.Systems
{
    public class TransformComponentSystem : ComponentSystemBase<Transform3d>
    {
        public TransformComponentSystem(IAjivaEcs ecs) : base(ecs)
        {
        }
    }
    public class Transform2dComponentSystem : ComponentSystemBase<Transform2d>
    {
        public Transform2dComponentSystem(IAjivaEcs ecs) : base(ecs)
        {
        }
    }
}
