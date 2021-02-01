using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ajiva.Ecs.System;
using ajiva.Helpers;

namespace ajiva.Worker
{
    public class WorkerPool : SystemBase
    {
        private const int DefaultWorkerCount = 16;
        public static WorkerPool Default = new(DefaultWorkerCount, nameof(Default));

        private readonly Worker[] workers;
        internal int _available;
        private int Available { get; set; }
        internal readonly object AvailableLock = new();
        public string Name { get; set; }

        private readonly ConcurrentQueue<WorkInfo> concurrentQueue = new();

        public WorkerPool(int workerCount, string name)
        {
            Name = name;
            Available = 0;
            workers = new Worker[workerCount];
            for (var i = 0; i < workerCount; i++)
            {
                workers[i] = new(this, i);
                workers[i].Start();
            }
        }

        public void EnqueueWork(Work work, ErrorNotify errorNotify, string name, object? userParam = default)
        {
            var wi = new WorkInfo(work, name, errorNotify, userParam);
            concurrentQueue.Enqueue(wi);

            Interlocked.Increment(ref _available);
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
            Console.WriteLine($"Open Workers: {workers.Length} Work: {_available}".FillUp(Console.BufferWidth - 1));
            for (var i = 0; i < workers.Length; i++)
            {
                var ci = i;
                workers[ci].State.Subscribe(delegate(WorkResult result)
                {
                    var print = $"{nameof(Worker)} {workers[ci].WorkerId.ToString(format)} {$" [{result.ToString()}] ~> {workers[ci].WorkName}"}";
                    Console.SetCursorPosition(0, posStart + ci + header);
                    Console.Write(print.FillUp(Console.BufferWidth - 1));
                }, cancellationToken);
            }
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            Enabled = false;
            Interlocked.Add(ref _available, 100);
        }

        /// <inheritdoc />
        protected override void Setup()
        {
        }
    }
}
