using ajiva.Ecs;
using Autofac;

public class UpdateManager : IUpdateManager, ILifetimeManager
{
    private readonly ContainerProxy _container;
    private readonly IEntityRegistry _entityRegistry;
    private readonly PeriodicUpdateRunner _updateRunner;
    private readonly ILogger _logger;

    public UpdateManager(ContainerProxy container, IEntityRegistry entityRegistry, PeriodicUpdateRunner updateRunner,
        ILogger logger)
    {
        _container = container;
        _entityRegistry = entityRegistry;
        _updateRunner = updateRunner;
        _logger = logger;
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
        _logger.Information("Starting UpdateManager");
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
        _logger.Information("PendingWorkItemCount: {PendingWorkItemCount}, EntitiesCount: {EntitiesCount}", ThreadPool.PendingWorkItemCount, _entityRegistry.EntitiesCount);
        _logger.Information(new string('-', 100));
        foreach (var (key, value) in updateDatas)
        {
            _logger.Information($"[ITERATION:{value.Iteration:X8}] | {value.Iteration.ToString(),-8}| {key.GetType().Name,-40}: Delta: {new TimeSpan(value.Delta):G}");
        }
    }

    public void IssueClose()
    {
        Task.Run(Stop);
    }
}
