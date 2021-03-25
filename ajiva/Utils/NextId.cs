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
            for (uint i = 0; i < MaxId; i++)
            {
                if (UsedIds.Contains(i)) continue;

                UsedIds.Add(i);
                return i;
            }
            throw new IndexOutOfRangeException($"For {typeof(T).FullName} the Maximum Id Limit is Reached!");
        }

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
