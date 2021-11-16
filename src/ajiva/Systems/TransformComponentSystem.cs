using ajiva.Components.Transform;
using ajiva.Ecs;

namespace ajiva.Systems;

public class TransformComponentSystem : ComponentSystemBase<Transform3d>
{
    private Random r = new Random();

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
