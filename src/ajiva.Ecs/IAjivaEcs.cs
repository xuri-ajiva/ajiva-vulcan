#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace ajiva.Ecs;

public interface IAjivaEcs : IDisposingLogger, IAjivaEcsObject
{
#region Entity

    void RegisterEntity<T>(T entity) where T : class, IEntity;
    bool TryUnRegisterEntity<T>(uint id, [MaybeNullWhen(false)] out T entity) where T : IEntity;
    bool TryUnRegisterEntity<T>(T entity) where T : IEntity;

#endregion

#region Component

    T RegisterComponent<T>(IEntity entity, Type type, T component) where T : class, IComponent;
    T UnRegisterComponent<T>(IEntity entity, Type type, T component) where T : class, IComponent;

#endregion


#region Live

#region Count

    long EntitiesCount { get; }
    long ComponentsCount { get; }

#endregion
    void IssueClose();
    void RegisterUpdate(IUpdate update);
    void StartUpdates();
    void WaitForExit();

#endregion
    T CreateAndRegisterEntity<T>() where T : class, IEntity;
}
