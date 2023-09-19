using System.Diagnostics;

namespace Ajiva.Ecs.Utils;

public record PeriodicUpdateInfo(TimeSpan Interval);
public class PeriodicUpdateRunner
{
    private readonly Dictionary<IUpdate, UpdateData> _updateData = new Dictionary<IUpdate, UpdateData>();

    public bool Running { get; private set; }

    public UpdateData RegisterUpdate(IUpdate update)
    {
        var data = new UpdateData();
        var runner = new Thread(() => { RunDelta(update, data); }) {
            Name = $"Update Runner for {update}"
        };
        data.Runner = runner;
        lock (_updateData)
        {
            _updateData.Add(update, data);
        }
        if (Running)
            runner.Start();
        return data;
    }

    public async Task UnRegisterUpdate(IUpdate update)
    {
        UpdateData? data;
        lock (_updateData)
        {
            if (_updateData.TryGetValue(update, out data))
            {
                data.Source.Cancel();
                _updateData.Remove(update);
            }
        }
        if (data is not null)
            while (data.Runner?.IsAlive ?? false)
                await Task.Delay(1);
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
            var remaining = ticks - elapsed;
            if (remaining / 10000 > 0) Thread.Sleep((int)(remaining / 10000) - 1);
            while (Stopwatch.GetTimestamp() - now < ticks) Thread.Yield();

            var end = Stopwatch.GetTimestamp();
            data.Delta = end - now;
            now = end;
        }
    }

    public void Start(IUpdate update)
    {
        lock (_updateData)
        {
            _updateData[update].Runner?.Start();
        }
    }

    public void Start()
    {
        Running = true;
        lock (_updateData)
        {
            foreach (var data in _updateData) data.Value.Runner?.Start();
        }
    }

    public void Stop()
    {
        Running = false;
        lock (_updateData)
        {
            foreach (var data in _updateData)
                data.Value.Source.Cancel();
        }
    }

    public void Cancel(IUpdate update)
    {
        _updateData[update].Source.Cancel();
    }

    public async Task WaitHandle(Action<Dictionary<IUpdate, UpdateData>> logStatus, CancellationToken cancellation)
    {
        var begin = DateTime.Now;
        foreach (var (key, value) in _updateData.ToArray())
            while (value.Runner?.IsAlive ?? false)
            {
                if ((DateTime.Now - begin).Seconds > 20)
                {
                    begin = DateTime.Now;
                    lock (_updateData)
                    {
                        logStatus.Invoke(_updateData);
                    }
                }

                await Task.Delay(10, cancellation);
                if (cancellation.IsCancellationRequested) return;
            }
    }

    public IEnumerable<IUpdate> GetUpdating()
    {
        return _updateData.Keys.ToList();
    }

    public class UpdateData
    {
        public readonly CancellationTokenSource Source;
        public long Delta;
        public long Iteration;
        public Thread? Runner;

        public UpdateData()
        {
            Source = new CancellationTokenSource();
            Iteration = 0L;
        }
    }
}