﻿#define __INextId_CHECK_ID
namespace Ajiva.Utils;

public interface INextId<T>
{
    private static readonly ISet<uint> UsedIds = new SortedSet<uint>();
    private static readonly object @lock = new object();

    private static uint lastId;
    public static uint MaxId = int.MaxValue;

    public static uint Next()
    {
        lock (@lock)
        {
            for (var i = lastId + 1; i != lastId; i++)
            {
                if (i >= MaxId) i = 0;

                if (UsedIds.Contains(i)) continue;

                UsedIds.Add(i);
                lastId = i;
                return i;
            }
        }
        throw new IndexOutOfRangeException($"For {typeof(T).FullName} the Maximum Id Limit is Reached!");
    }

    public static void Remove(uint id)
    {
#if __INextId_CHECK_ID
        if (UsedIds.Contains(id))
        {
#endif
            UsedIds.Remove(id);
#if __INextId_CHECK_ID
        }
        else
        {
            throw new ArgumentException("The id was not Use!", nameof(id));
        }
#endif
    }
}