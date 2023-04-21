namespace Ajiva;

public class Reactive<T> where T : IEquatable<T>
{
    public delegate void OnChangeDelegate(ref T? oldState, ref T? newState);

    private T? value;

    public Reactive(T? value)
    {
        this.value = value;
    }

    public T? Value
    {
        get => value;
        set
        {
            if (this.value is not null && this.value.Equals(value)) return;
            var copy = this.value;
            this.value = value;
            OnChange?.Invoke(ref copy, ref value);
        }
    }

    public event OnChangeDelegate? OnChange;

    public static explicit operator T?(Reactive<T> reactive)
    {
        return reactive.Value;
    }

    public static explicit operator Reactive<T>(T? value)
    {
        return new Reactive<T>(value);
    }
}
