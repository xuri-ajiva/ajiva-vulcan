using Ajiva.Ecs;
using Autofac;

namespace Ajiva.Application;

public class UpdateManager : IUpdateManager, ILifetimeManager
{
    private readonly ContainerProxy _container;
    private readonly IEntityRegistry _entityRegistry;
    private readonly ILogger _logger;
    private readonly PeriodicUpdateRunner _updateRunner;

    public UpdateManager(
        ContainerProxy container, IEntityRegistry entityRegistry, PeriodicUpdateRunner updateRunner,
        ILogger logger)
    {
        _container = container;
        _entityRegistry = entityRegistry;
        _updateRunner = updateRunner;
        _logger = logger;
    }

    public void IssueClose()
    {
        Task.Run(Stop);
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
            _updateRunner.RegisterUpdate(registration);
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

    private void LogStatus(Dictionary<IUpdate, PeriodicUpdateRunner.UpdateData> updateDatas)
    {
        //Console.Clear();
        _logger.Information("PendingWorkItemCount: {PendingWorkItemCount}, EntitiesCount: {EntitiesCount}", ThreadPool.PendingWorkItemCount, _entityRegistry.EntitiesCount);
        _logger.Information("--------------------");
        foreach (var (key, value) in updateDatas)
            _logger.Information("[ITERATION:{Iteration}] | {S}| {Name}: Delta: {Delta}", value.Iteration.ToString("X8"), value.Iteration.ToString().PadLeft(5), key.GetType().Name.PadLeft(30), new TimeSpan(value.Delta));
    }
}
