using System;

namespace ajiva.Utils.Changing
{
    public static class ChangedObserverExtensions
    {
        public static bool HasChanges(this IChangingObserver observer) => observer.ChangedAmount > 0;

        public static void RaiseChanged<T>(this IChangingObserver observer, ref T field, T value)
        {
            if (field is not null && (value is null || field.GetHashCode() == value.GetHashCode())) return;

            observer.Changed();
            field = value;
        }

        public static void RaiseChanged<T>(this IChangingObserver observer, T value, ref T field)
        {
            if (value is not null && (field is null || field.GetHashCode() == value.GetHashCode())) return;

            observer.Changed();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="cycle"></param>
        /// <returns>If object should be updated</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static bool UpdateCycle(this IChangingObserver observer, ulong cycle)
        {
            if (observer.ChangedAmount <= 0) return false;

            if (observer.ChangeBeginCycle == 0)
                observer.ChangeBeginCycle = cycle;

            return observer.Mode switch
            {
                ChangingCacheMode.DirectUpdate => true,
                ChangingCacheMode.ThisCycleUpdate => true,
                ChangingCacheMode.NextCycleUpdate => observer.ChangeBeginCycle + 1 >= cycle,
                ChangingCacheMode.AfterTenCycleUpdate => observer.ChangeBeginCycle + 10 >= cycle,
                ChangingCacheMode.ManualUpdate => false,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}