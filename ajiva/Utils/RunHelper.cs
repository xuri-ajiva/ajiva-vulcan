using System;
using System.Diagnostics;
using System.Threading;

namespace ajiva.Helpers
{
    public record UpdateInfo(TimeSpan Delta, ulong Iteration);

    public class RunHelper
    {
        public delegate bool DeltaRun(UpdateInfo info);

        public static void RunDelta(DeltaRun action, TimeSpan maxToRun)
        {
            ConsoleBlock block = new(1);

            var iteration = 0ul;
            var start = DateTime.Now;

            var delta = TimeSpan.Zero;
            var now = Stopwatch.GetTimestamp();
            UpdateInfo info = new(TimeSpan.Zero, 0);

            while (true)
            {
                Thread.Sleep(1);

                if (!action.Invoke(info with {Delta = delta, Iteration = iteration})) return;

                iteration++;

                if (iteration % 100 == 0)
                {
                    block.WriteAt($"iteration: {iteration}, delta: {delta}, FPS: {1000.0f / delta.TotalMilliseconds}, PendingWorkItemCount: {ThreadPool.PendingWorkItemCount}",0);

                    if (DateTime.Now - start > maxToRun)
                    {
                        return;
                    }
                }

                var end = Stopwatch.GetTimestamp();
                delta = new(end - now);

                now = end;
            }
        }
    }
}
