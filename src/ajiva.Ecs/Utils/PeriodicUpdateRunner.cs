using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Ajiva.Wrapper.Logger;

namespace ajiva.Ecs.Utils;

public record PeriodicUpdateInfo(TimeSpan Interval);

public class PeriodicUpdateRunner
{
    private class UpdateData
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

    public void RegisterUpdate(IUpdate update)
    {
        var data = new UpdateData();
        var runner = new Thread(() =>
        {
            RunDelta(update, data);
        }) { Name = $"Update Runner for {update}" };
        data.Runner = runner;
        updateDatas.Add(update, data);
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
        updateDatas[update].Runner.Start();
    }

    public void Cancel(IUpdate update)
    {
        updateDatas[update].Source.Cancel();
    }

    public async Task WaitHandle(CancellationToken cancellation)
    {
        DateTime begin = DateTime.Now;
        foreach (var (key, value) in updateDatas)
        {
            while (value.Runner.IsAlive)
            {
                if ((DateTime.Now - begin).Seconds > 20)
                {
                    begin = DateTime.Now;
                    LogStatus();
                }

                await Task.Delay(10, cancellation);
                if (cancellation.IsCancellationRequested) return;
            }
        }
    }

    private void LogStatus()
    {
        ALog.Info(new string('-', 100));
        foreach (var (key, value) in updateDatas)
        {
            ALog.Info($"[ITERATION:{value.Iteration:X8}] | {value.Iteration.ToString(),-8}| {key.GetType().Name,-40}: Delta: {new TimeSpan(value.Delta):G}");
        }
    }
}
