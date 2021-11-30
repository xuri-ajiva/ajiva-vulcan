using System.Collections;
using GlmSharp;

namespace ajiva.Components.Transform.Kd;

public class KdVec3 : IKdVec, IReadOnlyList<float> , IEquatable<KdVec3>
{
    private vec3 value;

    public KdVec3(vec3 value)
    {
        this.value = value;
    }
    public int Dimensions => 3;

    public float this[int dimension]
    {
        get => dimension switch
        {
            0 => value.x,
            1 => value.y,
            2 => value.z,
            _ => 0
        };
        set
        {
            switch (dimension)
            {
                case 0:
                    this.value.x = value;
                    break;
                case 1:
                    this.value.y = value;
                    break;
                case 2:
                    this.value.z = value;
                    break;
            }
        }
    }

    /// <inheritdoc />
    public IEnumerator<float> GetEnumerator()
    {
        yield return value.x;
        yield return value.y;
        yield return value.z;
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public int Count => Dimensions;

    public static implicit operator vec3(KdVec3 kdVec) => kdVec.value;
    public static implicit operator KdVec3(vec3 vec3) => new KdVec3(vec3);

    /// <inheritdoc />
    public bool Equals(KdVec3 other)
    {
        return value.Equals(other.value);   
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is KdVec3 other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return value.GetHashCode();
    }
}
