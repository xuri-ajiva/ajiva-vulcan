#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace ajiva.Ecs;

public interface IAjivaEcs : IDisposingLogger, IInit, IAjivaEcsObjectContainer<IAjivaEcsObject>, IAjivaEcsObject
{
#region Entity

    void RegisterEntity<T>(T entity) where T : class, IEntity;
    bool TryUnRegisterEntity<T>(uint id, [MaybeNullWhen(false)] out T entity) where T : IEntity;
    bool TryUnRegisterEntity<T>(T entity) where T : IEntity;

#endregion

#region Component

    T RegisterComponent<T>(IEntity entity, Type type, T component) where T : class, IComponent;
    T UnRegisterComponent<T>(IEntity entity, Type type, T component) where T : class, IComponent;
    /*
    bool TryAttachComponentToEntity<T, TAs>(IEntity entity, T component) where TAs : IComponent where T : class, TAs;
    bool TryDetachComponentFromEntity<T>(IEntity entity, [MaybeNullWhen(false)] out T component) where T : class, IComponent;
    */

#endregion

#region Create

    T Create<T>(Func<Type, object?> missing) where T : class;

#endregion

#region Live

#region Count

    long EntitiesCount { get; }
    long ComponentsCount { get; }

#endregion
    void IssueClose();
    void RegisterUpdate(IUpdate update);
    void RegisterInit(IInit init);
    void StartUpdates();
    void WaitForExit();

#endregion
}
