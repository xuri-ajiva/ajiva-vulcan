namespace ajiva;

public class Reactive<T> where T : IEquatable<T>, new()
{
    public delegate void OnChangeDelegate(ref T oldState, ref T newState);

    public event OnChangeDelegate? OnChange;
    private T value;
    public T Value
    {
        get => value;
        set
        {
            if(this.value.Equals(value)) return;
            var copy = this.value;
            this.value = value;
            OnChange?.Invoke(ref copy, ref value);
        }
    }

    public Reactive(T value)
    {
        this.value = value;
    }
        
    public static implicit operator T(Reactive<T> reactive) => reactive.Value;
    public static implicit operator Reactive<T>(T value) => new Reactive<T>(value);
}