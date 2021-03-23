using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ajiva.Ecs.Utils;

namespace ajiva.Helpers
{
    //Unique Static Value Cache
    public static class UsVc<T>
    {
        private static readonly int Size1;
        private static readonly TypeKey Key1;

        static UsVc()
        {
            Size1 = Unsafe.SizeOf<T>();
            var hc = typeof(T).GetHashCode() ^ Size1;
            for (var i = 0; i < 1000 && UsVc.KeySet.Contains(hc); i++)
            {
                hc = unchecked(hc ^ i + i);
            }
            UsVc.KeySet.Add(hc);
            Key1 = (TypeKey)hc;
        }

        public static int Size => Size1;
        public static TypeKey Key => Key1;
    }
    //only to make int into an type
    public enum TypeKey : int
    {
    }

    public static class UsVc
    {
        public static HashSet<int> KeySet { get; } = new();

        public static TypeKey TypeKey<T>(T nb)
        {
            return UsVc<T>.Key;
        }
    }
}
