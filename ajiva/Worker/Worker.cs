using System;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace ajiva.Worker
{
    public class Worker
    {
        internal string WorkName { get; private protected set; }

        internal readonly int WorkerId;
        public WorkerPool WorkerPool { get; }
        private readonly Thread workingThread;

        public Worker(WorkerPool workerPool, in int workerId)
        {
            WorkerId = workerId;
            WorkerPool = workerPool;
            WorkName = "";

            workingThread = new(Work)
            {
                Name = $"WorkerThread {workerId.ToString()} from {WorkerPool.Name}",
                CurrentCulture = CultureInfo.InvariantCulture
            };
        }

        private void Work(object? state)
        {
            while (!exit)
            {
                //if (!WorkerPool.Enabled)
                //{
                //    State.Publish(WorkResult.Disabled);
                //    while (!WorkerPool.Enabled)
                //    {
                //        Thread.Sleep(10);
                //    }
                //}
                WorkInfo? work;
                State.Publish(WorkResult.Waiting);
                WorkerPool.SyncSemaphore.WaitOne();
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

        public IObservable<WorkResult> State { get; } = new Subject<WorkResult>();

        public void Start()
        {
            workingThread.Start();
        }

        private bool exit;

        ~Worker()
        {
            exit = true;
        }
    }
}
