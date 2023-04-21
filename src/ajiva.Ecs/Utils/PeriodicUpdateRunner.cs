using System.Diagnostics;

namespace Ajiva.Ecs.Utils;

public record PeriodicUpdateInfo(TimeSpan Interval);

public class PeriodicUpdateRunner
{
    public class UpdateData
    {
        public long Iteration;
        public Thread Runner;
        public readonly CancellationTokenSource Source;
        public long Delta;

        public UpdateData()
        {
            this.Source = new CancellationTokenSource();
            Iteration = 0L;
        }
    }

    private readonly Dictionary<IUpdate, UpdateData> updateDatas = new();

    public UpdateData RegisterUpdate(IUpdate update)
    {
        var data = new UpdateData();
        var runner = new Thread(() =>
        {
            RunDelta(update, data);
        }) { Name = $"Update Runner for {update}" };
        data.Runner = runner;
        lock (updateDatas)
            updateDatas.Add(update, data);
        if (Running)
            runner.Start();
        return data;
    }
    
    public async Task UnRegisterUpdate(IUpdate update)
    {
        UpdateData? data;
        lock (updateDatas)
        {
            if (updateDatas.TryGetValue(update, out data))
            {
                data.Source.Cancel();
                updateDatas.Remove(update);
            }
        }
        if (data is not null)
        {
            while (data.Runner.IsAlive)
            {
                await Task.Delay(1);
            }
        }
    }

    private void RunDelta(IUpdate update, UpdateData data)
    {
        data.Iteration = 0L;
        data.Delta = update.Info.Interval.Ticks;
        var now = Stopwatch.GetTimestamp();
        
        var ticks = update.Info.Interval.Ticks;
        while (!data.Source.IsCancellationRequested)
        {
            update.Update(new UpdateInfo(new TimeSpan(data.Delta), data.Iteration));

            data.Iteration++;
            var elapsed = Stopwatch.GetTimestamp() - now;
            var remaining = (ticks - elapsed);
            if (remaining / 10000 > 0)
            {
                Thread.Sleep((int)(remaining / 10000) - 1);
            }
            while (Stopwatch.GetTimestamp() - now < ticks)
            {
                Thread.Yield();
            }

            var end = Stopwatch.GetTimestamp();
            data.Delta = end - now;
            now = end;
        }
    }

    public void Start(IUpdate update)
    {
        lock (updateDatas)
            updateDatas[update].Runner.Start();
    }

    public void Start()
    {
        Running = true;
        lock (updateDatas)
        {
            foreach (var data in updateDatas)
            {
                data.Value.Runner.Start();
            }
        }
    }

    public void Stop()
    {
        Running = false;
        lock (updateDatas)
            foreach (var data in updateDatas)
            {
                data.Value.Source.Cancel();
            }
    }

    public bool Running { get; private set; }

    public void Cancel(IUpdate update)
    {
        updateDatas[update].Source.Cancel();
    }

    public async Task WaitHandle(Action<Dictionary<IUpdate, UpdateData>> logStatus, CancellationToken cancellation)
    {
        DateTime begin = DateTime.Now;
        foreach (var (key, value) in updateDatas.ToArray())
        {
            while (value.Runner.IsAlive)
            {
                if ((DateTime.Now - begin).Seconds > 20)
                {
                    begin = DateTime.Now;
                    lock (updateDatas)
                        logStatus.Invoke(updateDatas);
                }

                await Task.Delay(10, cancellation);
                if (cancellation.IsCancellationRequested) return;
            }
        }
    }

    public IEnumerable<IUpdate> GetUpdating()
    {
        return updateDatas.Keys.ToList();
    }
}
