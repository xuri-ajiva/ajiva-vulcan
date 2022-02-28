using ajiva.Components.Transform;
using ajiva.Components.Transform.Ui;
using ajiva.Ecs;
using ajiva.Systems.VulcanEngine.Interfaces;

namespace ajiva.Systems;

public class TransformComponentSystem : ComponentSystemBase<Transform3d>, ITransformComponentSystem
{
    private Random r = new Random();

    public TransformComponentSystem(IAjivaEcs ecs) : base(ecs)
    {
    }
}
public class Transform2dComponentSystem : ComponentSystemBase<UiTransform>, ITransform2dComponentSystem
{
    public Transform2dComponentSystem(IAjivaEcs ecs) : base(ecs)
    {
    }
}
