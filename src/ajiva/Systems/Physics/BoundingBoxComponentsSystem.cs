using System.Runtime.CompilerServices;
using ajiva.Components.Physics;
using ajiva.Components.Transform;
using ajiva.Ecs;
using GlmSharp;

namespace ajiva.Systems.Physics;

[Dependent(typeof(CollisionsComponentSystem))]
public class BoundingBoxComponentsSystem : ComponentSystemBase<BoundingBox>, IUpdate
{
    /// <inheritdoc />
    public BoundingBoxComponentsSystem(IAjivaEcs ecs) : base(ecs) { }

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        //DoPhysicFrame();
    }

    /// <inheritdoc />
    public PeriodicTimer Timer { get; } = new PeriodicTimer(TimeSpan.FromMilliseconds(30));


    public void DoPhysicFrame()
    {
        foreach (var (box1, e1) in ComponentEntityMap.Where(x => !x.Key.Collider.IsStatic))
        foreach (var (box2, e2) in ComponentEntityMap.Where(other => box1 != other.Key))
            DoCollision(box1, box2, e1, e2);
    }

    public void DoCollision(BoundingBox b1, BoundingBox b2, IEntity e1, IEntity e2)
    {
        if (!Intersect(b1, b2)) return;

        var resolved = ResolveColision(b1, b2);
        if (b2.Collider.IsStatic)
        {
            if (e1.TryGetComponent<Transform3d>(out var t))
                t.Position += resolved;
            b1.ModifyPositionRelative(resolved);
        }
        else
        {
            if (e1.TryGetComponent<Transform3d>(out var t1))
                t1.Position += resolved / 2;
            b1.ModifyPositionRelative(resolved / 2);

            if (e2.TryGetComponent<Transform3d>(out var t2))
                t2.Position -= resolved / 2;
            b2.ModifyPositionRelative(-resolved / 2);
        }
    }

    /*
     * sorce:
     *
function resolveCollision(A, B) {
// get the vectors to check against
var vX = (A.x + (A.w / 2))  - (B.x + (B.w / 2)),
    vY = (A.y + (A.h / 2)) - (B.y + (B.h / 2)),
    // Half widths and half heights of the objects
    ww2 = (A.w / 2) + (B.w / 2),
    hh2 = (A.h / 2) + (B.h / 2),
    colDir = "";

// if the x and y vector are less than the half width or half height,
// they we must be inside the object, causing a collision
if (Math.abs(vX) < ww2 && Math.abs(vY) < hh2) {
    // figures out on which side we are colliding (top, bottom, left, or right)
    var oX = ww2 - Math.abs(vX),
        oY = hh2 - Math.abs(vY);
    if (oX >= oY) {
        if (vY > 0) {
            colDir = "TOP";
            A.y += oY;
        } else {
            colDir = "BOTTOM";
            A.y -= oY;
        }
    } else {
        if (vX > 0) {
            colDir = "LEFT";
            A.x += oX;
        } else {
            colDir = "RIGHT";
            A.x -= oX;
        }
    }
}
return colDir; // If you need info of the side that collided
}
     */
    private vec3 ResolveColision(BoundingBox a, BoundingBox b)
    {
        var v = a.Center - b.Center;
        var hh = a.SizeHalf + b.SizeHalf;

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
    }

    /// <inheritdoc />
    public override BoundingBox RegisterComponent(IEntity entity, BoundingBox component)
    {
        component.Ecs = Ecs;
        if (entity.TryGetComponent<Transform3d>(out var transform)) component.Transform = transform;
        if (entity.TryGetComponent<CollisionsComponent>(out var collider)) component.Collider = collider;

        return base.RegisterComponent(entity, component);
    }

#region IntersectionTest

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Intersect(BoundingBox a, BoundingBox b)
    {
        return IntersectX(a, b) &&
               IntersectY(a, b) &&
               IntersectZ(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IntersectZ(BoundingBox a, BoundingBox b)
    {
        return a.MinPos.z < b.MaxPos.z && a.MaxPos.z > b.MinPos.z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IntersectY(BoundingBox a, BoundingBox b)
    {
        return a.MinPos.y < b.MaxPos.y && a.MaxPos.y > b.MinPos.y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IntersectX(BoundingBox a, BoundingBox b)
    {
        return a.MinPos.x < b.MaxPos.x && a.MaxPos.x > b.MinPos.x;
    }

    private static bool IsPointInsideAabb(vec3 point, BoundingBox box)
    {
        return point.x > box.MinPos.x && point.x < box.MaxPos.x && point.y > box.MinPos.y && point.y < box.MaxPos.y && point.z > box.MinPos.z && point.z < box.MaxPos.z;
    }

#endregion
}