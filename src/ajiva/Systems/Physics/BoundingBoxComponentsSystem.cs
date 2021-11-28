using System.Runtime.CompilerServices;
using ajiva.Components.Physics;
using ajiva.Ecs;

namespace ajiva.Systems.Physics;

[Dependent(typeof(CollisionsComponentSystem))]
public class BoundingBoxComponentsSystem : ComponentSystemBase<IBoundingBox>, IUpdate
{
    /// <inheritdoc />
    public BoundingBoxComponentsSystem(IAjivaEcs ecs) : base(ecs) { }

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        //DoPhysicFrame();
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromMilliseconds(30));

    public void DoPhysicFrame()
    {
        foreach (var (box1, e1) in ComponentEntityMap.Where(x => !x.Key.Collider.IsStatic))
        foreach (var (box2, e2) in ComponentEntityMap.Where(other => box1 != other.Key))
            DoCollision(box1, box2, e1, e2);
    }

    public void DoCollision(IBoundingBox b1, IBoundingBox b2, IEntity e1, IEntity e2)
    {
        if (!Intersect(b1, b2)) return;

        ALog.Info($"Intersecting: {e1}, {e2} at {b1} {b2}");
        /*var resolved = ResolveColision(b1, b2);
        if (b2.Collider.IsStatic)
        {
            if (e1.TryGetComponent<Transform3d>(out var t))
                t.Position += resolved;
            //b1.ModifyPositionRelative(resolved);
        }
        else
        {
            if (e1.TryGetComponent<Transform3d>(out var t1))
                t1.Position += resolved / 2;
            //b1.ModifyPositionRelative(resolved / 2);

            if (e2.TryGetComponent<Transform3d>(out var t2))
                t2.Position -= resolved / 2;
            //b2.ModifyPositionRelative(-resolved / 2);
        }*/
    }

    /*
    private vec3 ResolveColision(IBoundingBox a, IBoundingBox b)
    {
        var v = a.Center.Position - b.Center.Position;
        var hh = a.Center.Scale + b.Center.Scale;

        const float mm = 0.0001f;
        float rx = 0,
            ry = 0,
            rz = 0;

        if (!(Math.Abs(v.x) < hh.x) || !(Math.Abs(v.y) < hh.y) || !(Math.Abs(v.z) < hh.z)) return vec3.Zero;
        var o = hh - vec3.Abs(v);
        if (o.x >= o.y)
        {
            if (v.y > 0)
                ry += o.y;
            else
                ry -= o.y;
        }
        else if (o.x >= o.z)
        {
            if (v.z > 0)
                rz += o.z;
            else
                rz -= o.z;
        }
        //if (o.y >= o.z)
        else
        {
            if (v.x > 0)
                rx += o.x;
            else
                rx -= o.x;
        }
        return new vec3(rx, ry, rz);
    }*/

    /// <inheritdoc />
    public override IBoundingBox RegisterComponent(IEntity entity, IBoundingBox component)
    {
        component.ComputeBoxBackground();
        return base.RegisterComponent(entity, component);
    }

#region IntersectionTest

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Intersect(IBoundingBox a, IBoundingBox b)
    {
        if (a.MinPos.Dimensions != b.MinPos.Dimensions) throw new ArgumentException();
        for (var i = 0; i < a.MinPos.Dimensions; i++)
        {
            //return a.MinPos.x < b.MaxPos.x && a.MaxPos.x > b.MinPos.x;
            if (a.MinPos[i] < b.MaxPos[i] && a.MaxPos[i] > b.MinPos[i])
            {
                return true;
            }
        }
        return false;
    }

#endregion
}
