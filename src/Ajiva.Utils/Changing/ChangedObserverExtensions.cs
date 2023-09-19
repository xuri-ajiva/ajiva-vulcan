//TODO fix boxing in equal check
namespace Ajiva.Utils.Changing;

public static class ChangedObserverExtensions
{
    public static bool HasChanges(this IChangingObserver observer)
    {
        return observer.ChangedAmount > 0;
    }

    public static bool HasChanges(this IOverTimeChangingObserver observer)
    {
        return observer.ChangedAmount > 0;
    }

    public static void RaiseChanged<T>(this IOverTimeChangingObserver observer, ref T? field, T? value) where T : IEquatable<T>
    {
        if (field is not null && (value is null || field.Equals(value))) return;

        field = value;
        observer.Changed();
    }

    public static void RaiseChanged<T>(this IOverTimeChangingObserver observer, T? value, ref T? field) where T : IEquatable<T>
    {
        if (value is not null && (field is null || field.Equals(value))) return;

        observer.Changed();
    }

    /// <summary>
    /// </summary>
    /// <param name="observer"></param>
    /// <param name="cycle"></param>
    /// <returns>If object should be updated</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static bool UpdateCycle(this IOverTimeChangingObserver observer, long cycle)
    {
        if (observer.Locked || observer.ChangedAmount <= 0) return false;

        if (observer.ChangeBeginCycle == 0) observer.ChangeBeginCycle = cycle;
        if (observer.ChangeBeginCycle + observer.DelayUpdateFor > cycle) return false;
        //observer.Updated();
        return true;
    }

    public static void RaiseAndSetIfChanged<T>(this IChangingObserver observer, ref T? field, T? value) where T : IEquatable<T>
    {
        if (field is not null && (value is null || field.Equals(value))) return;

        field = value;
        observer.Changed();
    }

    public static void RaiseIfChanged<T>(this IChangingObserver observer, T? field, T? value) where T : IEquatable<T>
    {
        if (field is not null && (value is null || field.Equals(value))) return;

        observer.Changed();
    }

    public static void RaiseAndSetIfChanged<TChange, TSender, TValue>(this IChangingObserver<TSender, TValue> observer, ref TChange? field, TChange? value)
        where TSender : class where TValue : struct where TChange : IEquatable<TChange>
    {
        if (field is not null && (value is null || field.Equals(value))) return;

        var before = observer.Result();
        field = value;
        observer.Changed(before, observer.Result());
    }

    public static void RaiseIfChanged<TChange, TSender, TValue>(this IChangingObserver<TSender, TValue> observer, TChange? field, TChange? value, TValue before)
        where TSender : class where TValue : struct where TChange : IEquatable<TChange>
    {
        if (field is not null && (value is null || field.Equals(value))) return;

        observer.Changed(before, observer.Result());
    }

    public static void RaiseAndSetIfChanged<TChange, TSender, TValue>(this IChangingObserverOnlyAfter<TSender, TValue> observer, ref TChange? field, TChange? value)
        where TSender : class where TValue : struct where TChange : IEquatable<TChange>
    {
        if ((value is not null && value.Equals(field)) || (field is not null && field.Equals(value))) return;
        field = value;
        observer.Changed(observer.Result());
    }

    public static void RaiseIfChanged<TChange, TSender, TValue>(this IChangingObserverOnlyAfter<TSender, TValue> observer, TChange? field, TChange? value)
        where TSender : class where TValue : struct where TChange : IEquatable<TChange>
    {
        if (field is not null && (value is null || field.Equals(value))) return;

        observer.Changed(observer.Result());
    }

    public static void RaiseAndSetIfChanged<TChange, TValue>(this IChangingObserverOnlyValue<TValue> observer, ref TChange? field, TChange? value)
        where TValue : struct where TChange : IEquatable<TChange>
    {
        if ((value is not null && value.Equals(field)) || (field is not null && field.Equals(value))) return;
        field = value;
        observer.Changed(observer.Result());
    }

    public static void RaiseIfChanged<TChange, TValue>(this IChangingObserverOnlyValue<TValue> observer, TChange? field, TChange? value)
        where TValue : struct where TChange : IEquatable<TChange>
    {
        if (field is not null && (value is null || field.Equals(value))) return;

        observer.Changed(observer.Result());
    }
}