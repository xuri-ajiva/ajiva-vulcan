using ajiva.Components.Transform;
using ajiva.Ecs;

namespace ajiva.Systems;

public class TransformComponentSystem : ComponentSystemBase<Transform3d>, IUpdate
{
    private Random r = new Random();

    public TransformComponentSystem(IAjivaEcs ecs) : base(ecs)
    {
    }

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
