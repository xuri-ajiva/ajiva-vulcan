using ajiva.Components.Physics;

namespace ajiva.Systems.Physics;

[Dependent(typeof(TransformComponentSystem))]
public class CollisionsComponentSystem : ComponentSystemBase<CollisionsComponent>
{

}
