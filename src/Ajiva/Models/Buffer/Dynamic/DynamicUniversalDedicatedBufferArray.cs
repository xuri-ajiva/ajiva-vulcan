using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Ajiva.Systems.VulcanEngine.Interfaces;
using SharpVk;

namespace Ajiva.Models.Buffer.Dynamic;

public class DynamicUniversalDedicatedBufferArray<T> : DisposingLogger where T : unmanaged
{
    private readonly IDeviceSystem deviceSystem;

    private readonly HashSet<uint> freeed = new HashSet<uint>();
    private readonly int SizeOfT;

    private T[] _items;
    private int _version;
    private BitArray Changed;
    private BitArray Used;

    public DynamicUniversalDedicatedBufferArray(IDeviceSystem deviceSystem, int initialLength, BufferUsageFlags usageFlags = BufferUsageFlags.UniformBuffer)
    {
        Length = initialLength;
        Changed = new BitArray(initialLength, false);
        Used = new BitArray(initialLength, false);
        _items = new T[initialLength];

        //todo check if needed
        for (var i = 0; i < initialLength; i++)
            _items[i] = new T();

        SizeOfT = Unsafe.SizeOf<T>();

        this.deviceSystem = deviceSystem;
        Count = 0;
        _version = 0;

        Staging = new ResizableDedicatedBuffer(
            (uint)(Length * SizeOfT),
            deviceSystem,
            BufferUsageFlags.TransferSource,
            MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent
        );
        Uniform = new ResizableDedicatedBuffer(
            (uint)(Length * SizeOfT),
            deviceSystem,
            BufferUsageFlags.TransferDestination | usageFlags,
            MemoryPropertyFlags.DeviceLocal
        );
        Uniform.BufferResized.OnChanged += BufferResizedOnChanged;
        Staging.BufferResized.OnChanged += BufferResizedOnChanged;
        BufferResized = new ChangingObserver<DynamicUniversalDedicatedBufferArray<T>>(this);
    }

    public IChangingObserver<DynamicUniversalDedicatedBufferArray<T>> BufferResized { get; }

    public ResizableDedicatedBuffer Uniform { get; set; }

    public ResizableDedicatedBuffer Staging { get; set; }

    private void BufferResizedOnChanged(ResizableDedicatedBuffer sender)
    {
        Log.Debug("Buffer Resized: {sender}", sender.Current().Buffer.RawHandle.ToUInt64().ToString("x8"));
    }

    public void Update(int index, ActionRef<T> action)
    {
        ref var item = ref _items[index];
        action.Invoke(ref item);
        lock (this)
        {
            Changed[index] = true;
        }
    }

#region List

    public int Length { get; set; }

    /// <inheritdoc cref="List{T}.GetEnumerator" />
    public IEnumerator<T> GetEnumerator()
    {
        return _items.Take(Count).Where((t, i) => Used[i]).GetEnumerator();
    }

    public void Resize(int newLength)
    {
        lock (this)
        {
            Length = newLength;
            var newItems = new T[newLength];
            if (Count > 0) Array.Copy(_items, newItems, _items.Length);
            _items = newItems;
            deviceSystem.WaitIdle();
            Staging.Resize((uint)(newLength * SizeOfT));
            Uniform.Resize((uint)(newLength * SizeOfT));
            Used.Length = newLength;
            Changed.Length = newLength;
            FullUpdate();
            BufferResized.Changed();
        }
    }

    /// <summary>
    ///     Adds An new element
    /// </summary>
    /// <param name="item"></param>
    /// <returns>index of element</returns>
    public uint Add(T item)
    {
        lock (this)
        {
            _version++;
            var index = GetFirstUnusedIndex();
            if (index >= (uint)_items.Length) Grow((int)(index + 1));
            _items[index] = item;
            Used[(int)index] = true;
            Changed[(int)index] = true;
            Count += 1;
            return index;
        }
    }

    private uint GetFirstUnusedIndex()
    {
        if (freeed.Any())
        {
            var ret = freeed.First();
            freeed.Remove(ret);
            return ret;
        }
        var i = Count;
        for (; i < Used.Length; i++)
            if (!Used[i])
                return (uint)i;
        return (uint)i;
    }

    /// <summary>
    ///     Increase the capacity of this list to at least the specified <paramref name="capacity" />.
    /// </summary>
    /// <param name="capacity">The minimum capacity to ensure.</param>
    private void Grow(int capacity)
    {
        Debug.Assert(_items.Length < capacity);

        var newCapacity = _items.Length == 0 ? DefaultCapacity : 2 * _items.Length;

        // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
        // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
        if ((uint)newCapacity > Array.MaxLength) newCapacity = Array.MaxLength;

        // If the computed capacity is still less than specified, set to the original argument.
        // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
        if (newCapacity < capacity) newCapacity = capacity;

        Capacity = newCapacity;
    }

    private const int DefaultCapacity = 10;

    /// <inheritdoc cref="List{T}.Clear" />
    public void Clear()
    {
        lock (this)
        {
            Changed = new BitArray(0);
            Used = new BitArray(0);
            _items = Array.Empty<T>();
            Staging.Resize(0);
            Uniform.Resize(0);
        }
    }

    /// <summary>
    ///     Gets or sets the total number of elements the internal data structure can hold without resizing.
    /// </summary>
    /// <returns>
    ///     The number of elements that the <see cref="DynamicUniversalDedicatedBufferArray{T}" /> can contain before
    ///     resizing is required.
    /// </returns>
    /// <exception cref="ArgumentException">Capacity is set to a value that is less than Count</exception>
    public int Capacity
    {
        get => _items.Length;
        set
        {
            lock (this)
            {
                if (value < Count)
                {
                    Log.Warning("Setting Capacity Lower then size will not work");
                    throw new ArgumentOutOfRangeException(nameof(value), "Setting Capacity Lower then size will not work");
                }

                if (value == _items.Length) return;

                if (value <= 0)
                    Clear();
                else
                    Resize(value);
            }
        }
    }

    /// <inheritdoc cref="List{T}.Contains" />
    public bool Contains(T item)
    {
        return Count != 0 && IndexOf(item) != -1;
    }

    /// <inheritdoc cref="List{T}.CopyTo" />
    public void CopyTo(T[] array, int arrayIndex)
    {
        Array.Copy(_items, 0, array, arrayIndex, Count);
    }

    /// <inheritdoc cref="List{T}.Remove" />
    public bool Remove(T item)
    {
        lock (this)
        {
            var index = IndexOf(item);
            if (index < 0) return false;

            RemoveAt((uint)index);
            return true;
        }
    }

    /// <inheritdoc cref="List{T}.Count" />

    public int Count { get; private set; }

    public int IndexOf(T item)
    {
        return Array.IndexOf(_items, item, 0, Count);
    }

    /// <inheritdoc cref="List{T}.RemoveAt" />
    public void RemoveAt(uint index)
    {
        lock (this)
        {
            if (index >= (uint)Count) throw new IndexOutOfRangeException("Index Out of range");
            Count--;
            Used[(int)index] = false;
            freeed.Add(index);
            _items[index] = default!;
            _version++;
        }
    }

    public T this[uint index]
    {
        get => _items[index];
        set
        {
            lock (this)
            {
                if (!Used[(int)index])
                {
                    Changed[(int)index] = true;
                    Used[(int)index] = true;
                    Count++;
                    _version++;
                }
                _items[index] = value;
            }
        }
    }

#endregion

#region Buffer

    /// <inheritdoc />
    public void CommitChanges()
    {
        lock (this)
        {
            var memPtr = Staging.MapDisposer();
            var simple = new List<Regions>();

            var cur = new Regions();

            for (var i = 0; i < _items.Length; i++) // go throw all known values
            {
                if (!Changed[i]) continue; // skip if not changed

                CopyValue(i, memPtr.Ptr); // update value in Staging buffer

                if (i - cur.End > cur.Length) // check if last region is close enough
                {
                    simple.Add(cur);
                    cur = new Regions(i, i);
                }
                else
                {
                    cur.Extend(i);
                }
            }

            simple.Add(cur);

            var regions = simple
                .Where(x => x.Length > 0)
                .Select(x => new BufferCopy {
                    Size = (ulong)(SizeOfT * x.Length),
                    DestinationOffset = (ulong)(SizeOfT * x.Begin),
                    SourceOffset = (ulong)(SizeOfT * x.Begin)
                }).ToArray();

            memPtr.Dispose(); // free address space

            Staging.CopyToRegions(Uniform, regions);

            Changed.SetAll(false);
        }
    }

    /// <inheritdoc />
    public void Commit(int index)
    {
        lock (this)
        {
            var memPtr = Staging.MapDisposer();
            CopyValue(index, memPtr.Ptr);
            memPtr.Dispose();
            Staging.CopyToRegions(Uniform, GetRegion(index));
        }
    }

    private void CopyValue(int index, IntPtr ptr)
    {
        unsafe
        {
            fixed (T* src = &_items[index])
            {
                *(T*)(ptr + SizeOfT * index) = *src;
            }
        }
        //Marshal.StructureToPtr(Value[index].Value, ptr + SizeOfT * index, true);
    }

    private void FullUpdate()
    {
        lock (this)
        {
            unsafe
            {
                var ptr = Staging.MapDisposer();
                fixed (T* src = &_items[0])
                {
                    Unsafe.CopyBlock(ptr.ToPointer(), src, (uint)(SizeOfT * _items.Length));
                }
                ptr.Dispose();

                Staging.CopyToRegions(Uniform, new BufferCopy(0, 0, Staging.Size));

                Changed.SetAll(false);
            }
        }
    }

    private BufferCopy GetRegion(int index)
    {
        return new BufferCopy {
            Size = (ulong)SizeOfT,
            DestinationOffset = (ulong)(SizeOfT * index),
            SourceOffset = (ulong)(SizeOfT * index)
        };
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        Staging.Dispose();
        Uniform.Dispose();
        _items = Array.Empty<T>();
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

#endregion
}