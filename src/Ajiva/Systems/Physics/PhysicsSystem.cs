using Ajiva.Components.Physics;

namespace Ajiva.Systems.Physics;

public class PhysicsSystem : ComponentSystemBase<PhysicsComponent>, IUpdate
{
    private bool enabled;

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        if (!enabled) return;
        foreach (var component in ComponentEntityMap.ToList()) component.Key.Update(delta.Delta);
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromMilliseconds(16));

    public void ResolveCollision(IEntity a, IEntity b)
    {
        if (!a.HasComponent<PhysicsComponent>() || !b.HasComponent<PhysicsComponent>())
            return;

        var aPhysics = a.Get<PhysicsComponent>();
        var bPhysics = b.Get<PhysicsComponent>();

        /*if(aPhysics.CollisionMask == 0 || bPhysics.CollisionMask == 0)
            return;*/

        aPhysics.DoCollisionResponse(bPhysics);
    }

    public void SetEnabled(bool physicsUpdated)
    {
        enabled = physicsUpdated;
    }

    public override PhysicsComponent CreateComponent(IEntity entity)
    {
        return new PhysicsComponent();
    }
}