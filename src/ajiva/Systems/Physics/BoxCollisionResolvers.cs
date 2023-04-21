using System.Numerics;
using ajiva.Components.Transform.SpatialAcceleration;

namespace ajiva.Systems.Physics;

internal static class BoxCollisionResolvers
{
    public static Random Random = new Random();

    /*public static Vector3 Default(StaticOctalSpace a, StaticOctalSpace b)
    {
        var axis = AxisResolve(a, b);
        return axis + Vector3.Random(Random, -0.01f, 0.01f);
        //var radius = -FastCubeCollisionResolver(a, b)/2;
        //return glm.Mix(axis ,radius, new Vector3(.2f));
        //return -FastCubeCollisionResolver(a, b)/2;
    }*/

    /// <summary>
    /// collision resolution between two cubes
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns>the vector to resolve the collision</returns>
    private static Vector3 FastCubeCollisionResolver(StaticOctalSpace a, StaticOctalSpace b)
    {
        var aCenter = a.Min + a.Size / 2;
        var bCenter = b.Min + b.Size / 2;

        var aHalfSize = a.Size / 2;
        var bHalfSize = b.Size / 2;

        var aMinB = aCenter - bHalfSize;
        var aMaxB = aCenter + bHalfSize;
        var bMinA = bCenter - aHalfSize;
        var bMaxA = bCenter + aHalfSize;

        var x = 0f;
        var y = 0f;
        var z = 0f;

        if (aMinB.X < bMinA.X)
        {
            x = bMinA.X - aMinB.X;
        }
        else if (aMaxB.X > bMaxA.X)
        {
            x = bMaxA.X - aMaxB.X;
        }

        if (aMinB.Y < bMinA.Y)
        {
            y = bMinA.Y - aMinB.Y;
        }
        else if (aMaxB.Y > bMaxA.Y)
        {
            y = bMaxA.Y - aMaxB.Y;
        }

        if (aMinB.Z < bMinA.Z)
        {
            z = bMinA.Z - aMinB.Z;
        }
        else if (aMaxB.Z > bMaxA.Z)
        {
            z = bMaxA.Z - aMaxB.Z;
        }

        return new Vector3(x, y, z);
    }

    private static Vector3 RadiusResolve(StaticOctalSpace a, StaticOctalSpace b)
    {
        var aMin = a.Min;
        var aMax = a.Max;
        var bMin = b.Min;
        var bMax = b.Max;

        var aCenter = (aMin + aMax) / 2;
        var bCenter = (bMin + bMax) / 2;

        var aSize = aMax - aMin;
        var bSize = bMax - bMin;

        var aRadius = aSize.Length() / 2;
        var bRadius = bSize.Length() / 2;

        var aToB = bCenter - aCenter;
        var aToBLength = aToB.Length();

        if (aToBLength > aRadius + bRadius)
        {
            return Vector3.Zero;
        }

        var aToBNormalized = aToB / aToBLength;

        var aToBProjection = aToBLength - aRadius - bRadius;

        var resolved = aToBNormalized * aToBProjection;

        return resolved;
    }

    private static Vector3 AxisResolve(StaticOctalSpace a, StaticOctalSpace b)
    {
        var aCenter = a.Position + a.Size / 2;
        var bCenter = b.Position + b.Size / 2;

        var v = aCenter - bCenter;
        var hh = a.Size / 2 + b.Size / 2;

        float rx = 0,
            ry = 0,
            rz = 0;

        if (!(Math.Abs(v.X) < hh.X) || !(Math.Abs(v.Y) < hh.Y) || !(Math.Abs(v.Z) < hh.Z)) return Vector3.Zero;
        var o = hh - Vector3.Abs(v);
        if (o.X >= o.Y)
        {
            if (v.Y > 0)
                ry += o.Y;
            else
                ry -= o.Y;
        }
        else if (o.X >= o.Z)
        {
            if (v.Z > 0)
                rz += o.Z;
            else
                rz -= o.Z;
        }
        //if (o.Y >= o.Z)
        else
        {
            if (v.X > 0)
                rx += o.X;
            else
                rx -= o.X;
        }
        return new Vector3(rx, ry, rz);
    }
}
