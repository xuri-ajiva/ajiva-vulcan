namespace ajiva.Utils
{
    public class ByRef<T> where T : unmanaged
    {
        public T Value;

        public ByRef(T value)
        {
            Value = value;
        }

        public static implicit operator T(ByRef<T> byRef) => byRef.Value;
        public static implicit operator ByRef<T>(T value) => new ByRef<T>(value);
    }
}
