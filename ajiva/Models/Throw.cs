using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ajiva.Models
{
    public static class Throw
    {
        public static void Assert([DoesNotReturnIf(false)] bool condition, string? message)
        {
            if (condition) return;

            var callingFrame = new StackTrace().GetFrame(1);
            throw new TypeInitializationException(callingFrame?.GetMethod()?.DeclaringType?.Name ?? "Unknown", new ArgumentException(message));
        }
    }
}
