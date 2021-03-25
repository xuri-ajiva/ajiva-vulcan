using System;
using System.Threading;

namespace ajiva.Utils
{
    public class Notify<T>
    {
        public event Action<T?, T?>? OnChanged;
        public T? innerValue;
        public T Value
        {
            get => innerValue;
            set
            {
                if (value != null && !value.Equals(innerValue)) return;
                Changed(innerValue, value);
                innerValue = value;
            }
        }

        public void Changed(T? oldValue, T? newValue)
        {
            OnChanged?.Invoke(oldValue, newValue);
        }

        public void Publish(T? newValue)
        {
            Changed(innerValue, newValue);
            innerValue = newValue;
        }

        public void Subscribe(Action<T?,T?> action, CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => OnChanged -= action);
            OnChanged += action;
        }
    }
}
