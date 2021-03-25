using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Ajiva.Wrapper.Logger;

namespace ajiva.Utils
{
    //Unique Static Value Cache
    public static class UsVc<T>
    {
        private static int Size1;
        private static TypeKey Key1;

        static UsVc()
        {
            if (UsVc.KeySet.ContainsValue(typeof(T)))
            {
                Size1 = Unsafe.SizeOf<T>();
                Key1 = UsVc.KeySetOtherWay[typeof(T)];
            }
            else
            {
                (Key1, Size1) = UsVc.Create<T>();
            }
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
        public static (TypeKey key, int size) Create<T>()
        {
            var size1 = Unsafe.SizeOf<T>();

            return (TypeKeyMain(typeof(T)), size1);
        }

        private static (TypeKey key, int size) Create(Type type)
        {
            var size1 = Marshal.SizeOf(type);

            return (TypeKeyMain(type), size1);
        }

        public static TypeKey TypeKeyMain(Type type)
        {
            if (KeySetOtherWay.ContainsKey(type))
                return KeySetOtherWay[type];

            var hc = (TypeKey)type.GetHashCode();
            for (var i = 0; i < 1000 && KeySet.ContainsKey(hc); i++)
            {
                hc = (TypeKey)unchecked((int)hc ^ i + i);
            }

            KeySet.Add(hc, type);
            KeySetOtherWay.Add(type, hc);

            LogHelper.Log($"TypeKey For: {type} = {hc}");
            return hc;
        }

        public static Dictionary<TypeKey, Type> KeySet { get; } = new();
        public static Dictionary<Type, TypeKey> KeySetOtherWay { get; } = new();

        public static TypeKey TypeKeyFor<T>(T nb)
        {
            return UsVc<T>.Key;
        }

        public static TypeKey TypeKeyForType(Type type)
        {
            return KeySet.ContainsValue(type)
                ? KeySetOtherWay[type]
                : TypeKeyMain(type);
        }
    }
}
