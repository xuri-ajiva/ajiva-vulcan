using System.Globalization;

namespace ajiva.Worker;

public class Worker
{
    internal readonly int WorkerId;
    private readonly Thread workingThread;

    private bool exit;

    public Worker(WorkerPool workerPool, in int workerId)
    {
        WorkerId = workerId;
        WorkerPool = workerPool;
        WorkName = "";

        State.Publish(WorkResult.Waiting);
        workingThread = new Thread(Work)
        {
            Name = $"WorkerThread {workerId.ToString()} from {WorkerPool.Name}",
            CurrentCulture = CultureInfo.InvariantCulture
        };
    }

    internal string WorkName { get; private protected set; }
    public WorkerPool WorkerPool { get; }

    public Notify<WorkResult> State { get; } = new Notify<WorkResult>();

    private void Work(object? state)
    {
        while (!exit)
        {
            WorkInfo? work;
            State.Publish(WorkResult.Waiting);
            WorkerPool.SyncSemaphore.WaitOne();
            if (WorkerPool.CancellationTokenSource.IsCancellationRequested)
                return;

            lock (WorkerPool.AvailableLock)
            {
                State.Publish(WorkResult.Locking);

                if (!WorkerPool.TryGetWork(out work)) continue;
            }
            if (work == null) continue;

            WorkName = work.Name;
            work.ActiveWorker = this;
            State.Publish(WorkResult.Working);
            var result = work.Invoke();
            State.Publish(result);
        }
    }

    public void Start()
    {
        workingThread.Start();
    }

    ~Worker()
    {
        exit = true;
    }
}