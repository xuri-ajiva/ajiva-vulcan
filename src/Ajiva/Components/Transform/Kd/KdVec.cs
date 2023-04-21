namespace Ajiva.Components.Transform.Kd;

public struct KdVec : IKdVec, IKdVecReadOnly
{
    public int Dimensions => values.Length;
    private readonly float[] values;

    public KdVec(int dimensions)
    {
        values = new float[dimensions];
    }

    public KdVec(params float[] values)
    {
        this.values = values;
    }

    /// <inheritdoc cref="IKdVec.this" />
    public float this[int dimension]
    {
        get => dimension > values.Length ? 0 : values[dimension];
        set
        {
            if (values.Length > dimension)
                values[dimension] = value;
        }
    }

    public static KdVec operator -(KdVec lhs, IKdVec rhs)
    {
        if (lhs.Dimensions != rhs.Dimensions) throw new ArgumentException();

        var res = new KdVec(lhs.Dimensions);
        for (var i = 0; i < lhs.Dimensions; i++) res.values[i] = lhs.values[i] - rhs[i];

        return res;
    }

    public static KdVec operator /(KdVec lhs, IKdVec rhs)
    {
        if (lhs.Dimensions != rhs.Dimensions) throw new ArgumentException();

        var res = new KdVec(lhs.Dimensions);
        for (var i = 0; i < lhs.Dimensions; i++) res.values[i] = lhs.values[i] / rhs[i];

        return res;
    }

    public static KdVec operator *(KdVec lhs, IKdVec rhs)
    {
        if (lhs.Dimensions != rhs.Dimensions) throw new ArgumentException();

        var res = new KdVec(lhs.Dimensions);
        for (var i = 0; i < lhs.Dimensions; i++) res.values[i] = lhs.values[i] * rhs[i];

        return res;
    }

    public static KdVec operator /(KdVec lhs, float scale)
    {
        var res = new KdVec(lhs.Dimensions);
        for (var i = 0; i < lhs.Dimensions; i++) res.values[i] = lhs.values[i] / scale;

        return res;
    }

    public static KdVec operator *(KdVec lhs, float scale)
    {
        var res = new KdVec(lhs.Dimensions);
        for (var i = 0; i < lhs.Dimensions; i++) res.values[i] = lhs.values[i] * scale;

        return res;
    }

    public static KdVec operator +(KdVec lhs, IKdVec rhs)
    {
        if (lhs.Dimensions != rhs.Dimensions) throw new ArgumentException();

        var res = new KdVec(lhs.Dimensions);
        for (var i = 0; i < lhs.Dimensions; i++) res.values[i] = lhs.values[i] + rhs[i];

        return res;
    }

    public void Update(KdVec kdVec)
    {
        if (Dimensions != kdVec.Dimensions) throw new ArgumentException();
        for (var i = 0; i < Dimensions; i++) values[i] = kdVec.values[i];
    }

    public void Update(params float[] updated)
    {
        if (Dimensions != updated.Length) throw new ArgumentException();
        for (var i = 0; i < Dimensions; i++) values[i] = updated[i];
    }
}