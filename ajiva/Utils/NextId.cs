#define __INextId_CHECK_ID
using System;
using System.Collections.Generic;

namespace ajiva.Utils
{
    public interface INextId<T>
    {
        private static readonly HashSet<uint> UsedIds = new();


        public static uint Next()
        {
            for (var i = lastId + 1; i != lastId; i++)
            {
                if (i >= MaxId) i = 0;
                
                if (UsedIds.Contains(i)) continue;

                UsedIds.Add(i);
                lastId = i;
                return i;
            }
            throw new IndexOutOfRangeException($"For {typeof(T).FullName} the Maximum Id Limit is Reached!");
        }

        public static uint lastId;
        public static uint MaxId = int.MaxValue;

        // ReSharper disable once UnusedMember.Global
#pragma warning disable 414
        private static T type = default;
#pragma warning restore 414
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
}
