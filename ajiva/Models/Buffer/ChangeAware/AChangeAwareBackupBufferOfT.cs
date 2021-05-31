using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;
using SharpVk;

namespace ajiva.Models.Buffer.ChangeAware
{
    public class AChangeAwareBackupBufferOfT<T> : DisposingLogger, IAChangeAwareBackupBufferOfT<T> where T : struct, IComp<T>
    {
        public AChangeAwareBackupBufferOfT(int length, DeviceSystem deviceSystem)
        {
            Length = length;
            Changed = new(length);
            Value = new T[length];
            SizeOfT = UsVc<T>.Size;

            this.deviceSystem = deviceSystem;
            currentMax = length;

            Staging = new((uint)(Length * SizeOfT));
            Uniform = new((uint)(Length * SizeOfT));

            Staging.Create(deviceSystem, BufferUsageFlags.TransferSource, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent);
            Uniform.Create(deviceSystem, BufferUsageFlags.TransferDestination | BufferUsageFlags.UniformBuffer, MemoryPropertyFlags.DeviceLocal);
        }

        private DeviceSystem deviceSystem { get; set; }

        private int currentMax;

        public T this[in int index]
        {
            get => Value[index];
            set => Set(index, value);
        }
        
        /// <inheritdoc />
        public int Length { get; }

        /// <inheritdoc />
        public int SizeOfT { get; }

        /// <inheritdoc />
        public ABuffer Uniform { get; }

        /// <inheritdoc />
        public ABuffer Staging { get; }

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
            var memPtr = Staging.MapDisposer();
            List<Regions> simple = new();

            Regions cur = new();

            for (var i = 0; i < currentMax; i++) // go throw all known values
            {
                if (!Changed[i]) continue; // skip if not changed

                Marshal.StructureToPtr(Value[i], memPtr.Ptr + SizeOfT * i, true); // update value in Staging buffer

                if (i - cur.End > cur.Length) // check if last region is close enough
                {
                    simple.Add(cur);
                    cur = new(i, i);
                }
                else
                {
                    cur.Extend(i);
                }
            }

            simple.Add(cur);

            var regions = simple
                .Where(x => x.Length > 0)
                .Select(x => new BufferCopy
                {
                    Size = (ulong)(SizeOfT * x.Length),
                    DestinationOffset = (ulong)(SizeOfT * x.Begin),
                    SourceOffset = (ulong)(SizeOfT * x.Begin),
                }).ToArray();

            memPtr.Dispose(); // free address space

            Staging.CopyRegions(Uniform, regions, deviceSystem);

            Changed.SetAll(false);
        }

        /// <inheritdoc />
        public void Commit(int index)
        {
            var memPtr = Staging.MapDisposer();
            Marshal.StructureToPtr(Value[index], memPtr.Ptr + SizeOfT * index, true);
            memPtr.Dispose();
            Staging.CopyRegions(Uniform, GetRegion(index), deviceSystem);
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

        private BufferCopy GetRegion(int index)
        {
            return new()
            {
                Size = (ulong)SizeOfT,
                DestinationOffset = (ulong)(SizeOfT * index),
                SourceOffset = (ulong)(SizeOfT * index),
            };
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            Staging.Dispose();
            Uniform.Dispose();
        }

        private struct Regions
        {
            public Regions(int begin, int end)
            {
                Begin = begin;
                End = end;
                initialized = true;
            }

            public int Begin;
            public int End;
            private bool initialized;
            public int Length => End - Begin + 1;

            public void Extend(int end)
            {
                if (!initialized)
                {
                    Begin = end;
                    initialized = true;
                }
                End = end;
            }
        }
    }
}
