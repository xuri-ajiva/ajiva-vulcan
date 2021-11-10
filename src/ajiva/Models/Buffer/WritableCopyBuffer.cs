namespace ajiva.Models.Buffer;

public class WritableCopyBuffer<T> : CopyBuffer<T> where T : struct, IComp<T>
{
    /// <inheritdoc />
    public WritableCopyBuffer(T[] val) : base(val)
    {
    }

    public void Update(T[] newData)
    {
        if (newData.Length > Value.Length)
        {
            throw new ArgumentException("Currently you can only update the data, not add some", nameof(newData));
        }

        for (var i = 0; i < newData.Length; i++)
        {
            Value[i] = newData[i];
        }
        //Value = newData;
        CopyValueToBuffer();
    }

    public new T this[in uint index]
    {
        get => this[(int)index];
        set => this[(int)index] = value;
    }
    public new T this[in int index]
    {
        get
        {
            if (index > Length)
                throw new ArgumentOutOfRangeException(nameof(index), index, "");
            return base[index];
        }
        set
        {
            if (index > Length)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Currently you can only update the data, not add some");

            if (GetRef(index).CompareTo(value))
                return;

            base[index] = value;
            CopySingleValueToBuffer(index);
        }
    }

    public void Update(T newData, int id)
    {
        if (id > Value.Length)
        {
            throw new ArgumentException("Currently you can only update the data, not add some", nameof(newData));
        }

        Value[id] = newData;
        CopySingleValueToBuffer(id);
    }
}