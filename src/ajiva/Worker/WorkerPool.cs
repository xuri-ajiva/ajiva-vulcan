using System.Collections.Concurrent;
using ajiva.Ecs;

namespace ajiva.Worker;

public class WorkerPool : SystemBase
{

    private readonly Worker[] workers;

    public Semaphore SyncSemaphore { get; } = new(0, int.MaxValue);
    internal readonly object AvailableLock = new();
    public string Name { get; set; }

    private readonly ConcurrentQueue<WorkInfo> concurrentQueue = new();

    public CancellationTokenSource CancellationTokenSource { get; }= new();

    public WorkerPool(int workerCount, string name, IAjivaEcs ecs) : base(ecs)
    {
        Name = name;
        workers = new Worker[workerCount];

        for (var i = 0; i < workerCount; i++)
            workers[i] = new(this, i);

        //StartMonitoring(CancellationTokenSource.Token);

        for (var i = 0; i < workerCount; i++)
            workers[i].Start();
    }

    public void EnqueueWork(Work work, ErrorNotify errorNotify, string name, object? userParam = default)
    {
        var wi = new WorkInfo(work, name, errorNotify, userParam);
        concurrentQueue.Enqueue(wi);

        SyncSemaphore.Release(1);
    }

    public bool Enabled { get; set; }

    public bool TryGetWork(out WorkInfo? workInfo)
    {
        return concurrentQueue.TryDequeue(out workInfo);
    }

    public void StartMonitoring(CancellationToken cancellationToken)
    {
        var block = new ConsoleBlock(workers.Length + 2);

        block.WriteAt("Monitoring Started...", 0);
        var format = "X" + (workers.Length - 1).ToString("X").Length;

        for (var i = 0; i < workers.Length; i++)
        {
            var ci = i;
            workers[ci].State.Subscribe(delegate(WorkResult _, WorkResult result)
            {
                block.WriteAt($"Open Workers: {workers.Length} Work: {concurrentQueue.Count}", 1);
                block.WriteAt($"{nameof(Worker)} {workers[ci].WorkerId.ToString(format)} [{result.ToString()}] ~> {workers[ci].WorkName}", ci + 2);
            }, cancellationToken);
        }
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        CancellationTokenSource.Cancel();
        Enabled = false;
        SyncSemaphore.Release(workers.Length * 5);
        SyncSemaphore.Dispose();
    }
}