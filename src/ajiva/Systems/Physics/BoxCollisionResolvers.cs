using ajiva.Components.Transform.SpatialAcceleration;
using GlmSharp;

namespace ajiva.Systems.Physics;

internal static class BoxCollisionResolvers
{
    public static Random Random = new Random();

    public static vec3 Default(StaticOctalSpace a, StaticOctalSpace b)
    {
        var axis = AxisResolve(a, b);
        return axis + vec3.Random(Random, -0.01f, 0.01f);
        //var radius = -FastCubeCollisionResolver(a, b)/2;
        //return glm.Mix(axis ,radius, new vec3(.2f));
        //return -FastCubeCollisionResolver(a, b)/2;
    }

    /// <summary>
    /// collision resolution between two cubes
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns>the vector to resolve the collision</returns>
    private static vec3 FastCubeCollisionResolver(StaticOctalSpace a, StaticOctalSpace b)
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

        if (aMinB.x < bMinA.x)
        {
            x = bMinA.x - aMinB.x;
        }
        else if (aMaxB.x > bMaxA.x)
        {
            x = bMaxA.x - aMaxB.x;
        }

        if (aMinB.y < bMinA.y)
        {
            y = bMinA.y - aMinB.y;
        }
        else if (aMaxB.y > bMaxA.y)
        {
            y = bMaxA.y - aMaxB.y;
        }

        if (aMinB.z < bMinA.z)
        {
            z = bMinA.z - aMinB.z;
        }
        else if (aMaxB.z > bMaxA.z)
        {
            z = bMaxA.z - aMaxB.z;
        }

        return new vec3(x, y, z);
    }

    private static vec3 RadiusResolve(StaticOctalSpace a, StaticOctalSpace b)
    {
        var aMin = a.Min;
        var aMax = a.Max;
        var bMin = b.Min;
        var bMax = b.Max;

        var aCenter = (aMin + aMax) / 2;
        var bCenter = (bMin + bMax) / 2;

        var aSize = aMax - aMin;
        var bSize = bMax - bMin;

        var aRadius = aSize.Length / 2;
        var bRadius = bSize.Length / 2;

        var aToB = bCenter - aCenter;
        var aToBLength = aToB.Length;

        if (aToBLength > aRadius + bRadius)
        {
            return vec3.Zero;
        }

        var aToBNormalized = aToB / aToBLength;

        var aToBProjection = aToBLength - aRadius - bRadius;

        var resolved = aToBNormalized * aToBProjection;

        return resolved;
    }

    private static vec3 AxisResolve(StaticOctalSpace a, StaticOctalSpace b)
    {
        var aCenter = a.Position + a.Size / 2;
        var bCenter = b.Position + b.Size / 2;

        var v = aCenter - bCenter;
        var hh = a.Size / 2 + b.Size / 2;

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
}
