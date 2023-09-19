namespace Ajiva.Worker;

public interface IWorkerPool
{
    Semaphore SyncSemaphore { get; }
    string Name { get; set; }
    CancellationTokenSource CancellationTokenSource { get; }
    bool Enabled { get; set; }
    bool Disposed { get; }
    void EnqueueWork(Work work, ErrorNotify errorNotify, string name, object? userParam = default);
    bool TryGetWork(out WorkInfo? workInfo);
    void StartMonitoring(CancellationToken cancellationToken);
}