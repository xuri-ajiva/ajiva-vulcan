using System;
using System.Runtime.CompilerServices;

namespace ajiva.Models
{
    public class BufferOfT<T> : ABuffer where T : struct
    {
        public int Length { get; }
        public uint SizeOfT { get; }
        protected T[] Value { get; set; }

        /// <inheritdoc />
        public BufferOfT(T[] val) : base((uint)(Unsafe.SizeOf<T>() * val.Length))
        {
            Value = val;
            Length = val.Length;
            SizeOfT = (uint)Unsafe.SizeOf<T>();
        }

        public void ClearT()
        {
            Value = null!;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            ClearT();
            base.ReleaseUnmanagedResources();
        }
    }
}
