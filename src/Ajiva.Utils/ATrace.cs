using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Ajiva.Utils;

public static class ATrace
{
    public static void Assert([DoesNotReturnIf(false)] bool condition, string? message)
    {
        if (condition) return;

        var callingFrame = new StackTrace().GetFrame(1);
        throw new TypeInitializationException(callingFrame?.GetMethod()?.DeclaringType?.Name ?? "Unknown", new ArgumentException(message));
    }

    public static ConcurrentDictionary<Type, long> Instances = new();
    public static readonly Collection<Type> FullLog = new();
    public static Collection<Type> Log = new();

    public static void LogDeconstructed(Type type)
    {
        Instances.AddOrUpdate(type, _ => 0, (_, l) => l - 1);

        if (Log.Contains(type))
            Serilog.Log.Debug($"Deletion of Type {type}, Count {Instances[type]}");
        if (FullLog.Contains(type))
            Serilog.Log.Debug($"Deletion of Type {type}, Count {Instances[type]}, Stack:\n" + GetStack());
    }

    public static void LogCreated(Type type)
    {
        var tReal = type;
        while (tReal.BaseType != null)
        {
            if (tReal.BaseType == typeof(object))
                break;
            tReal = tReal.BaseType;
        }

        Instances.AddOrUpdate(tReal, _ => 1, (_, l) => l + 1);

        if (Log.Contains(tReal))
            Serilog.Log.Debug($"New Creation of Type {type}, Count {Instances[tReal]}");
        if (FullLog.Contains(tReal))
            Serilog.Log.Debug($"New Creation of Type {type}, Count {Instances[tReal]}, Stack:\n" + GetStack());
    }

    public static string GetStack(int skip = 2) => string.Join("", new StackTrace(true).GetFrames().Skip(skip).Select(x => x.ToString()));

    public static void PrintStack() => Console.Write(GetStack());

    public static void LockInline(string value)
    {
        var (left, top) = Console.GetCursorPosition();
        Console.Write(value );
        Console.Write("                     ");
        Console.SetCursorPosition(left, top);
    }
}
