using System;
using System.Collections;
using System.Runtime.InteropServices;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;
using SharpVk;

namespace ajiva.Models.Buffer.ChangeAware
{
    public class AChangeAwareBufferOfT<T> : DisposingLogger, IAChangeAwareBufferOfT<T> where T : struct
    {
        public AChangeAwareBufferOfT(int length, DeviceSystem deviceSystem, BufferUsageFlags usage, MemoryPropertyFlags flags)
        {
            Length = length;
            Changed = new(length);
            Value = new T[length];
            SizeOfT = UsVc<T>.Size;
            Buffer = new((uint)(SizeOfT * length));
            Buffer.Create(deviceSystem, usage, flags);
        }

        public AChangeAwareBufferOfT(T[] value, DeviceSystem deviceSystem, BufferUsageFlags usage, MemoryPropertyFlags flags)
        {
            Length = value.Length;
            Changed = new(value.Length);
            Value = value;
            SizeOfT = UsVc<T>.Size;
            Buffer = new((uint)(SizeOfT * Length));
            Buffer.Create(deviceSystem, usage, flags);
        }

        private int currentMax;

        /// <inheritdoc />
        public int Length { get; }

        /// <inheritdoc />
        public int SizeOfT { get; }

        /// <inheritdoc />
        public ABuffer Buffer { get; }

        /// <inheritdoc />
        public T[] Value { get; }

        /// <inheritdoc />
        public BitArray Changed { get; }

        /// <inheritdoc />
        public void Set(int index, T value)
        {
            if (index > currentMax)
            {
                if (index > Length)
                {
                    //todo resize array if to small
                    throw new IndexOutOfRangeException("Currently not resizable!");
                }
                currentMax = index;
            }
            Value[index] = value;
            Changed[index] = true;
        }

        /// <inheritdoc />
        public void CommitChanges()
        {
            using var memPtr = Buffer.MapDisposer();
            for (var i = 0; i < currentMax; i++)
            {
                if (Changed[i])
                {
                    Marshal.StructureToPtr(Value[i], memPtr.Ptr + SizeOfT * i, true);
                }
            }
            memPtr.Dispose();
            Changed.SetAll(false);
        }

        /// <inheritdoc />
        public void Commit(int index)
        {
            using var memPtr = Buffer.MapDisposer();
            Marshal.StructureToPtr(Value[index], memPtr.Ptr + SizeOfT * index, true);
            memPtr.Dispose();
        }

        /// <inheritdoc />
        public ref T GetRef(int index)
        {
            return ref Value[index];
        }

        /// <inheritdoc />
        public void SetChanged(int index, bool changed)
        {
            Changed[index] = changed;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            Buffer.Dispose();
        }
    }
}
