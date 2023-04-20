using ajiva.Components.Mesh;
using ajiva.Components.Physics;

namespace ajiva.Systems.Physics;

public class CollisionsComponentSystem : ComponentSystemBase<CollisionsComponent>
{
    private readonly MeshPool _meshPool;

    public CollisionsComponentSystem(MeshPool meshPool)
    {
        _meshPool = meshPool;
    }
    public override CollisionsComponent CreateComponent(IEntity entity) => new CollisionsComponent(_meshPool);
}
