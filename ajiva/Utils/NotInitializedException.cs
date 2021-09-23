using System;

namespace ajiva.Utils
{
    public class NotInitializedException : Exception
    {
        public NotInitializedException(string name, object? obj) : base($"The {obj?.GetType().Name} was not initialized", obj is null ? new NullReferenceException($"{name} was null!") : new ArgumentException($"{obj.GetType()} has an filed that is null"))
        {
        }
    }
}
