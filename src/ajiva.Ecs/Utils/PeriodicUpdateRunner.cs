using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ajiva.Ecs.Utils;

public class PeriodicUpdateRunner
{
    private record UpdateData(Thread Runner, CancellationTokenSource Source);

    private Dictionary<IUpdate, UpdateData> UpdateDatas = new();

    public void RegisterUpdate(IUpdate update)
    {
        var source = new CancellationTokenSource();
        var runner = new Thread(() =>
        {
            RunDelta(update, source.Token);
        }) { Name = $"Update Runner for {update}" };
        UpdateDatas.Add(update, new UpdateData(runner, source));
    }

    private void RunDelta(IUpdate update, CancellationToken cancellationToken)
    {
        var iteration = 0L;
        var delta = update.Info.Interval.Ticks;
        var now = Stopwatch.GetTimestamp();

        var ticks = update.Info.Interval.Ticks;
        while (!cancellationToken.IsCancellationRequested)
        {
            update.Update(new UpdateInfo(new TimeSpan(delta), iteration));

            iteration++;
            while (Stopwatch.GetTimestamp() - now < ticks)
            {
                Thread.Yield();
            }

            var end = Stopwatch.GetTimestamp();
            delta = end - now;
            now = end;
        }
    }

    public void Start(IUpdate update)
    {
        UpdateDatas[update].Runner.Start();
    }

    public void Cancel(IUpdate update)
    {
        UpdateDatas[update].Source.Cancel();
    }

    public async Task WaitHandle(CancellationToken cancellation)
    {
        foreach (var (key, value) in UpdateDatas)
        {
            while (value.Runner.IsAlive)
            {
                await Task.Delay(10, cancellation);
                if (cancellation.IsCancellationRequested) return;
            }
        }
    }
}
