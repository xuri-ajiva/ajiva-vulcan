using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Ajiva.Wrapper.Logger;

namespace ajiva.Ecs;

public class AjivaEcs : DisposingLogger, IAjivaEcs
{
    public CancellationTokenSource CancellationTokenSource { get; }
    private static readonly Type Me = typeof(AjivaEcs);

    private readonly object @lock = new object();
    private uint currentEntityId;

    public AjivaEcs(CancellationTokenSource cancellationTokenSource)
    {
        CancellationTokenSource = cancellationTokenSource;
        AddResolver<AjivaEcsDefaultResolver>();
    }

    public Dictionary<uint, IEntity> Entities { get; } = new Dictionary<uint, IEntity>();

    /// <inheritdoc />
    public long EntitiesCount => Entities.Count;

    /// <inheritdoc />
    public long ComponentsCount => 0; /* Entities.Sum(x => x.Value.Components.Count);*/ //sequence failed

#region Add

    /// <inheritdoc />
    public void RegisterEntity<T>(T entity) where T : class, IEntity
    {
        Entities.Add(entity.Id, entity);
    }

    /// <inheritdoc />
    public T RegisterComponent<T>(IEntity entity, T component) where T : class, IComponent
    {
        return Get<T, IComponentSystem<T>>().RegisterComponent(entity, component);
    }

    /// <inheritdoc />
    public bool TryAttachComponentToEntity<T, TAs>(IEntity entity, T component)  where TAs : IComponent where T : class, TAs
    {
        entity.AddComponent<T, TAs>(component);
        RegisterComponent(entity, component);
        return true;
    }

    /// <inheritdoc />
    public bool TryDetachComponentFromEntity<T>(IEntity entity, [MaybeNullWhen(false)] out T component) where T : class, IComponent
    {
        if (!entity.TryRemoveComponent<T>(out var ctnt))
        {
            component = default;
            return false;
        }
        component = UnRegisterComponent(entity, (T)ctnt);
        return true;
    }

#endregion

#region Create

    /// <inheritdoc />
    public T Create<T>(Func<Type, object?> missing) where T : class
    {
        return ObjectContainer.Create<T>();
    }

    /// <inheritdoc />
    public bool TryUnRegisterEntity<T>(uint id, out T entity) where T : IEntity
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public bool TryUnRegisterEntity<T>(T entity) where T : IEntity
    {
        throw new NotImplementedException();
    }

#endregion

#region Delete

    /// <inheritdoc />
    public T UnRegisterComponent<T>(IEntity entity, T component) where T : class, IComponent
    {
        return Get<T, IComponentSystem<T>>().UnRegisterComponent(entity, component);
    }

#endregion

#region Live

    /// <inheritdoc />
    public void Init()
    {
        var isInti = new List<IInit> { this };
        foreach (var init in ObjectContainer.GetAllAssignableTo<IInit>())
        {
            InitOne(init, isInti);
        }
    }

    /// <inheritdoc />
    private void InitOne(IInit toInit, ICollection<IInit> initDone)
    {
        if (initDone.Contains(toInit)) return;
        var attrib = toInit.GetType().GetCustomAttributes(typeof(DependentAttribute), false).FirstOrDefault();

        if (attrib is DependentAttribute dependent)
        {
            foreach (var type in dependent.Dependent)
            {
                var typeInterfaces = type.GetInterfaces();
                if (!typeInterfaces.Any(x => x == typeof(IInit))) continue;

                InitOne(ObjectContainer.Get<IInit>(type), initDone);
            }
        }
        LogHelper.Log($"Init: {toInit.GetType()}");
        toInit.Init();
        initDone.Add(toInit);
    }

    /// <inheritdoc />
    public void IssueClose()
    {
        lock (@lock)
        {
            CancellationTokenSource.Cancel();
            UpdateRunner.Stop();
        }
    }

    public bool DisposingInprogress = false;

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        lock (@lock)
        {
            if (DisposingInprogress) return;
            DisposingInprogress = true;
            foreach (var update in UpdateRunner.GetUpdating())
            {
                UpdateRunner.Cancel(update);
            }
            UpdateRunner.WaitHandle(LogStatus, CancellationToken.None).Wait();

            foreach (var entity in Entities.Values)
            {
                entity.Dispose();
            }
            Entities.Clear();

            ObjectContainer.Dispose();
        }
    }

    /// <inheritdoc />
    private static void DisposeRec(ISystem toDispose, IDictionary<Type, ISystem> disposingSet)
    {
        foreach (var (_, system) in disposingSet)
        {
            var attrib = system.GetType().GetCustomAttributes(typeof(DependentAttribute), false).FirstOrDefault();
            if (attrib is not DependentAttribute dependent) continue;
            if (!dependent.Dependent.Any(x => x == toDispose.GetType())) continue;

            DisposeRec(system, disposingSet);
        }
        toDispose.Dispose();
    }

    /// <inheritdoc />
    public void RegisterUpdate(IUpdate update)
    {
        UpdateRunner.RegisterUpdate(update);
    }

    /// <inheritdoc />
    public void RegisterInit(IInit init)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void StartUpdates()
    {
        foreach (var update in ObjectContainer.GetAllAssignableTo<IUpdate>())
        {
            UpdateRunner.RegisterUpdate(update);
        }
        UpdateRunner.Start();
    }

    /// <inheritdoc />
    public void WaitForExit()
    {
        UpdateRunner.WaitHandle(LogStatus, CancellationTokenSource.Token).Wait();
    }

    private PeriodicUpdateRunner UpdateRunner = new();

    private void LogStatus(Dictionary<IUpdate, PeriodicUpdateRunner.UpdateData> updateDatas)
    {
        ALog.Info($"PendingWorkItemCount: {ThreadPool.PendingWorkItemCount}, EntitiesCount: {EntitiesCount}");
        ALog.Info(new string('-', 100));
        foreach (var (key, value) in updateDatas)
        {
            ALog.Info($"[ITERATION:{value.Iteration:X8}] | {value.Iteration.ToString(),-8}| {key.GetType().Name,-40}: Delta: {new TimeSpan(value.Delta):G}");
        }
    }

#endregion

    private AjivaEcsObjectContainer<IAjivaEcsObject> ObjectContainer = new();

    /// <inheritdoc />
    public T Add<T, TAs>(T value) where T : class, IAjivaEcsObject where TAs : IAjivaEcsObject
    {
        return ObjectContainer.Add<T, TAs>(value);
    }

    /// <inheritdoc />
    public void Add(Type type, IAjivaEcsObject value)
    {
        ObjectContainer.Add(type, value);
    }

    /// <inheritdoc />
    public TAs Get<T, TAs>() where T : class, IAjivaEcsObject where TAs : IAjivaEcsObject
    {
        return ObjectContainer.Get<T, TAs>();
    }

    /// <inheritdoc />
    public TAs Get<TAs>(Type type) where TAs : IAjivaEcsObject
    {
        return ObjectContainer.Get<TAs>(type);
    }

    /// <inheritdoc />
    public void AddResolver<T>() where T : class, IAjivaEcsResolver, new()
    {
        ObjectContainer.AddResolver<T>();
    }

    /// <inheritdoc />
    public T Create<T>() where T : class
    {
        return ObjectContainer.Create<T>();
    }

    /// <inheritdoc />
    public object Inject(Type type)
    {
        return ObjectContainer.Inject(type);
    }
}
