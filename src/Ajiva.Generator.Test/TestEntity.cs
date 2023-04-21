using System.Diagnostics.CodeAnalysis;
using Ajiva.Ecs.Entity.Helper;

namespace Ajiva.Test;

public interface IInterface
{
    public string S { get; set; }
}
public interface IComponent
{
}
public class Component3 : IInterface, IComponent
{
    /// <inheritdoc />
    public string S { get; set; }
}
public interface IComponent1 : IComponent
{
    
}
public class Component1 : IComponent1, IComponent
{
    public Component1(int value)
    {
        Value = value;
    }

    public int Value { get; set; }
}
[EntityComponent(typeof(IComponent1), typeof(Component3))]
public partial class TestEntity
{
    public TestEntity()
    {
        Component1 = new Component1(99);
    }

    public void Dispose()
    {
    }
}
public interface IEntity : IDisposable
{
    //uint Id { get; }
    bool TryGetComponent<T>([MaybeNullWhen(false)] out T value) where T : IComponent;

    bool HasComponent<T>() where T : IComponent;

    //bool TryRemoveComponent<T>([MaybeNullWhen(false)] out IComponent component) where T : IComponent;
    //T AddComponent<T, TAs>(T component) where TAs : IComponent where T : class, TAs;
    T Get<T>() where T : IComponent;
    //object Get(Type type);
}
