﻿using System.Collections.Concurrent;

namespace Ajiva.Components;

public class ObjectPool<T> where T : class, IRespectable, new()
{
    private readonly ConcurrentBag<T> _objects = new ConcurrentBag<T>();
    public long MaxSize { get; set; } = 5000;
    public static ObjectPool<T> Instance { get; } = new ObjectPool<T>();

    public T Rent()
    {
        return /*_objects.TryTake(out var item) ? item :*/ new T();
    }

    public void Return(T? item)
    {
        /*if(item is null) return;
        item.Reset();
        if (_objects.Count < MaxSize)
        {
            _objects.Add(item);
        }*/
    }
}
public interface IRespectable
{
    void Reset();
}