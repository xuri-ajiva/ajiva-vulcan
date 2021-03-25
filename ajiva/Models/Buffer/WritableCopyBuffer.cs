using System;

namespace ajiva.Models
{
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

            for (int i = 0; i < newData.Length; i++)
            {
                Value[i] = newData[i];
            }
            //Value = newData;
            CopyValueToBuffer();
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
}
