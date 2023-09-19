using Ajiva.Components.Mesh;
using Ajiva.Components.Physics;

namespace Ajiva.Systems.Physics;

public class CollisionsComponentSystem : ComponentSystemBase<CollisionsComponent>
{
    private readonly MeshPool _meshPool;

    public CollisionsComponentSystem(MeshPool meshPool)
    {
        _meshPool = meshPool;
    }

    public override CollisionsComponent CreateComponent(IEntity entity)
    {
        return new CollisionsComponent(_meshPool);
    }
}