using System;
using System.Collections.Concurrent;
using System.Threading;
using ajiva.Ecs.System;
using ajiva.Helpers;

namespace ajiva.Worker
{
    public class WorkerPool : SystemBase
    {
        private const int DefaultWorkerCount = 16;
        public static WorkerPool Default = new(DefaultWorkerCount, nameof(Default));

        private readonly Worker[] workers;

        public Semaphore SyncSemaphore { get; } = new(0, int.MaxValue);
        internal readonly object AvailableLock = new();
        public string Name { get; set; }

        private readonly ConcurrentQueue<WorkInfo> concurrentQueue = new();

        private CancellationTokenSource cancellationTokenSource = new();

        public WorkerPool(int workerCount, string name)
        {
            Name = name;
            workers = new Worker[workerCount];

            for (var i = 0; i < workerCount; i++)
                workers[i] = new(this, i);

            StartMonitoring(cancellationTokenSource.Token);

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
            var posStart = Console.CursorTop;

            const int header = 2;
            Console.SetCursorPosition(0, posStart + 0);
            Console.WriteLine("Monitoring Started...");
            var format = "X" + workers.Length.ToString("X").Length;

            Console.SetCursorPosition(0, posStart + 1);
            Console.WriteLine($"Open Workers: {workers.Length} Work: {concurrentQueue.Count}".FillUp(Console.BufferWidth - 1));
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
        protected override void ReleaseUnmanagedResources()
        {
            Enabled = false;
            SyncSemaphore.Release(workers.Length * 5);
        }

        /// <inheritdoc />
        protected override void Setup()
        {
        }
    }
}
