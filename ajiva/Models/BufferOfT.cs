using System;
using System.Runtime.CompilerServices;

namespace ajiva.Models
{
    public class BufferOfT<T> : ABuffer where T : notnull
    {
        public int Length { get; }
        public uint SizeOfT { get; }
        public T[] Value { get; protected set; }

        /// <inheritdoc />
        public BufferOfT(T[] val) : base((uint)(Unsafe.SizeOf<T>() * val.Length))
        {
            Value = val;
            Length = val.Length;
            SizeOfT = (uint)Unsafe.SizeOf<T>();
        }

        public void ClearT()
        {
            Value = Array.Empty<T>();
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            ClearT();
            base.ReleaseUnmanagedResources();
        }
    }
}