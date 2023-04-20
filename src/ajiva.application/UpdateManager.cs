using ajiva.Ecs;
using ajiva.Ecs.Utils;
using Ajiva.Wrapper.Logger;
using Autofac;

public class UpdateManager : IUpdateManager, ILifetimeManager
{
    private readonly ContainerProxy _container;
    private readonly IEntityRegistry _entityRegistry;
    private readonly PeriodicUpdateRunner _updateRunner;

    public UpdateManager(ContainerProxy container, IEntityRegistry entityRegistry, PeriodicUpdateRunner updateRunner)
    {
        _container = container;
        _entityRegistry = entityRegistry;
        _updateRunner = updateRunner;
    }

    public void RegisterUpdate(IUpdate update)
    {
        _updateRunner.RegisterUpdate(update);
    }

    public void UnRegisterUpdate(IUpdate update)
    {
        Task.Run(async () => await _updateRunner.UnRegisterUpdate(update));
    }

    public void RegisterUpdateForAllInContainer()
    {
        foreach (var registration in _container.Container.ComponentRegistry.Registrations
                     .Where(r => typeof(IUpdate).IsAssignableFrom(r.Activator.LimitType))
                     .Select(r => r.Activator.LimitType)
                     .Select(t => _container.Container.Resolve(t))
                     .OfType<IUpdate>()
                     .Distinct())
        {
            _updateRunner.RegisterUpdate(registration);
        }
    }

    public void Run()
    {
        _updateRunner.Start();
    }

    public async Task Wait(CancellationToken cancellation)
    {
        await _updateRunner.WaitHandle(LogStatus, cancellation);
    }

    public async Task Stop()
    {
        _updateRunner.Stop();
    }

    void LogStatus(Dictionary<IUpdate, PeriodicUpdateRunner.UpdateData> updateDatas)
    {
        ALog.Info($"PendingWorkItemCount: {ThreadPool.PendingWorkItemCount}, EntitiesCount: {_entityRegistry.EntitiesCount}");
        ALog.Info(new string('-', 100));
        foreach (var (key, value) in updateDatas)
        {
            ALog.Info($"[ITERATION:{value.Iteration:X8}] | {value.Iteration.ToString(),-8}| {key.GetType().Name,-40}: Delta: {new TimeSpan(value.Delta):G}");
        }
    }

    public void IssueClose()
    {
        Task.Run(Stop);
    }
}
