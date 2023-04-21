using System.Numerics;

namespace ajiva;

public static class MathX
{
    public static float Radians(this float v) => (float)(v * 0.0174532925199432957692369076848861271344287188854172f);
    public static float Degrees(this float v) => (float)(v * 57.295779513082320876798154814105170332405472466564321f);
    public static float HermiteInterpolationOrder3(this float v) => v * v * (3 - 2 * v);
    public static float HermiteInterpolationOrder5(this float v) => v * v * v * (6 * v * v - 15 * v + 10);

    public static float Clamp(this float v, float min = 0, float max = 1) => v < min
        ? min
        : v > max
            ? max
            : v;

    /// <summary>
    /// Returns a Vector3 from component-wise application of Mix (min * (1-a) + max * a).
    /// </summary>
    public static Vector3 Mix(Vector3 min, Vector3 max, Vector3 a) => new Vector3(Mix(min.X, max.X, a.X), Mix(min.Y, max.Y, a.Y), Mix(min.Z, max.Z, a.Z));

    /// <summary>
    /// Returns a Vector3 from component-wise application of Mix (min * (1-a) + max * a).
    /// </summary>
    public static float Mix(float min, float max, float a) => min * (1 - a) + max * a;

    /// <summary>
    /// Returns a Vector3 from component-wise application of Lerp (min * (1-a) + max * a).
    /// </summary>
    public static Vector3 Lerp(Vector3 min, Vector3 max, Vector3 a) => new Vector3(Lerp(min.X, max.X, a.X), Lerp(min.Y, max.Y, a.Y), Lerp(min.Z, max.Z, a.Z));

    /// <summary>
    /// Returns a Vector3 from component-wise application of Lerp (min * (1-a) + max * a).
    /// </summary>
    public static float Lerp(float min, float max, float a) => min * (1 - a) + max * a;

    /// <summary>
    /// Returns a Vector3 from component-wise application of Smoothstep (((v - edge0) / (edge1 - edge0)).Clamp().HermiteInterpolationOrder3()).
    /// </summary>
    public static Vector3 Smoothstep(Vector3 edge0, Vector3 edge1, Vector3 v) => new Vector3(Smoothstep(edge0.X, edge1.X, v.X), Smoothstep(edge0.Y, edge1.Y, v.Y), Smoothstep(edge0.Z, edge1.Z, v.Z));

    /// <summary>
    /// Returns a Vector3 from component-wise application of Smoothstep (((v - edge0) / (edge1 - edge0)).Clamp().HermiteInterpolationOrder3()).
    /// </summary>
    public static float Smoothstep(float edge0, float edge1, float v) => HermiteInterpolationOrder3(Clamp(((v - edge0) / (edge1 - edge0))));

    /// <summary>
    /// Returns a Vector3 from component-wise application of Smootherstep (((v - edge0) / (edge1 - edge0)).Clamp().HermiteInterpolationOrder5()).
    /// </summary>
    public static Vector3 Smootherstep(Vector3 edge0, Vector3 edge1, Vector3 v) => new Vector3(Smootherstep(edge0.X, edge1.X, v.X), Smootherstep(edge0.Y, edge1.Y, v.Y), Smootherstep(edge0.Z, edge1.Z, v.Z));

    /// <summary>
    /// Returns a Vector3 from component-wise application of Smootherstep (((v - edge0) / (edge1 - edge0)).Clamp().HermiteInterpolationOrder5()).
    /// </summary>
    public static float Smootherstep(float edge0, float edge1, float v) => HermiteInterpolationOrder5(Clamp((v - edge0) / (edge1 - edge0)));
}
