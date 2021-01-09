using System;

namespace ajiva.Models
{
    public class WritableCopyBuffer<T> : CopyBuffer<T> where T : notnull
    {
        /// <inheritdoc />
        public WritableCopyBuffer(T[] val) : base(val)
        {
        }

        public void Update(T[] newData)
        {
            if (newData.Length > Value.Length)
            {
                throw new ArgumentException("Curently you can only update the data, not add some", nameof(newData));
            }

            Value = newData;
            CopyValueToBuffer();
        }
    }
}