﻿using System.Numerics;
using ajiva.Components.Physics;
using ajiva.Components.Transform.SpatialAcceleration;
using ajiva.Worker;

namespace ajiva.Systems.Physics;

public class BoundingBoxComponentsSystem : ComponentSystemBase<BoundingBox>, IUpdate
{
    StaticOctalTreeContainer<BoundingBox> _octalTree;
    private readonly PhysicsSystem _physicsSystem;
    private readonly IWorkerPool _workerPool;
    private bool phisicsUpdated;

    /// <inheritdoc />
    public BoundingBoxComponentsSystem(PhysicsSystem physicsSystem, IWorkerPool workerPool)
    {
        _physicsSystem = physicsSystem;
        _workerPool = workerPool;
        var pos = new Vector3(float.MinValue / MathF.PI);
        _octalTree = new StaticOctalTreeContainer<BoundingBox>(new StaticOctalSpace(pos, pos * -MathF.E), 255);
    }

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        if (phisicsUpdated)
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
                if (ComponentEntityMap.TryGetValue(dynamicItem.Item, out var b1))
                {
                    if (ComponentEntityMap.TryGetValue(octalItem.Item, out var b2))
                    {
                        if (b1 != b2)
                        {
                            _physicsSystem.ResolveCollision(b1, b2);
                        }
                    }
                }
                //_physicsSystem.ResolveCollision(ComponentEntityMap[dynamicItem.Item], ComponentEntityMap[octalItem.Item]);
            }
        }
        //ALog.Debug("End DoPhysicFrame");

        /*
        foreach (var (box1, e1) in ComponentEntityMap.Where(x => !x.Key.Collider.IsStatic))
        foreach (var (box2, e2) in ComponentEntityMap.Where(other => box1 != other.Key))
            DoCollision(box1, box2, e1, e2);*/
    }

    /*
    private void DoCollision(StaticOctalItem<BoundingBox> dynamicItem, StaticOctalItem<BoundingBox> otherItem)
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
    }*/

    /// <inheritdoc />
    public override BoundingBox RegisterComponent(IEntity entity, BoundingBox component)
    {
        component.SetTree(_octalTree);
        component.ComputeBoxBackground();
        return base.RegisterComponent(entity, component);
    }

    /// <inheritdoc />
    public override BoundingBox UnRegisterComponent(IEntity entity, BoundingBox component)
    {
        component.RemoveTree();
        return base.UnRegisterComponent(entity, component);
    }

    public override BoundingBox CreateComponent(IEntity entity)
    {
        return new BoundingBox(entity, _workerPool);
    }

    public void TogglePhysicUpdate()
    {
        phisicsUpdated = !phisicsUpdated;
        _physicsSystem.SetEnabled(phisicsUpdated);
    }
}
