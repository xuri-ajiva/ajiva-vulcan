using ajiva.Components.Physics;
using ajiva.Components.Transform;
using ajiva.Components.Transform.SpatialAcceleration;
using ajiva.Ecs;
using GlmSharp;

namespace ajiva.Systems.Physics;

[Dependent(typeof(CollisionsComponentSystem))]
public class BoundingBoxComponentsSystem : ComponentSystemBase<IBoundingBox>, IUpdate
{
    StaticOctalTreeContainer<IBoundingBox> _octalTree;

    /// <inheritdoc />
    public BoundingBoxComponentsSystem(IAjivaEcs ecs) : base(ecs)
    {
        var pos = new vec3(float.MinValue / MathF.PI);
        _octalTree = new StaticOctalTreeContainer<IBoundingBox>(new StaticOctalSpace(pos, pos * -MathF.E), 255);
    }

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        DoPhysicFrame();
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromMilliseconds(30));

    public void DoPhysicFrame()
    {
        //ALog.Debug("Begin DoPhysicFrame");
        foreach (var dynamicItem in _octalTree.Items)
        {
            foreach (var octalItem in _octalTree.Search(dynamicItem.Space))
            {
                DoCollision(dynamicItem, octalItem);
            }
        }
        //ALog.Debug("End DoPhysicFrame");

        /*
        foreach (var (box1, e1) in ComponentEntityMap.Where(x => !x.Key.Collider.IsStatic))
        foreach (var (box2, e2) in ComponentEntityMap.Where(other => box1 != other.Key))
            DoCollision(box1, box2, e1, e2);*/
    }

    private void DoCollision(StaticOctalItem<IBoundingBox> dynamicItem, StaticOctalItem<IBoundingBox> otherItem)
    {
        if (dynamicItem.Item.Collider.IsStatic)
        {
            if (otherItem.Item.Collider.IsStatic) return;
            (dynamicItem, otherItem) = (otherItem, dynamicItem);
        }

        var resolved = BoxCollisionResolvers.Default(dynamicItem.Space, otherItem.Space) * 0.8f;

        if (otherItem.Item.Collider.IsStatic)
        {
            if (ComponentEntityMap.TryGetValue(dynamicItem.Item, out var b1) && b1.TryGetComponent<Transform3d>(out var t))
            {
                t.Position += resolved;
            }
        }
        else
        {
            if (ComponentEntityMap.TryGetValue(dynamicItem.Item, out var b1) && b1.TryGetComponent<Transform3d>(out var t))
            {
                t.Position += resolved / 2;
            }

            if (ComponentEntityMap.TryGetValue(otherItem.Item, out var b2) && b2.TryGetComponent<Transform3d>(out var t2))
            {
                t2.Position -= resolved / 2;
            }
        }
    }

    /// <inheritdoc />
    public override IBoundingBox RegisterComponent(IEntity entity, IBoundingBox component)
    {
        component.SetTree(_octalTree);
        component.ComputeBoxBackground();
        return base.RegisterComponent(entity, component);
    }

    /// <inheritdoc />
    public override IBoundingBox UnRegisterComponent(IEntity entity, IBoundingBox component)
    {
        component.RemoveTree();
        return base.UnRegisterComponent(entity, component);
    }
}
