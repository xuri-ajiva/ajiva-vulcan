using System;
using System.Diagnostics;
using System.Threading;

namespace ajiva.Helpers
{
    public class RunHelper
    {
        public delegate void DeltaRun(TimeSpan delta);

        public static void RunDelta(DeltaRun action, Func<bool> condition, TimeSpan maxToRun)
        {
            var iteration = 0u;
            var start = DateTime.Now;

            var delta = TimeSpan.Zero;
            long end = 0, now = Stopwatch.GetTimestamp();
            while (condition())
            {
                Thread.Sleep(1);

                action?.Invoke(delta);

                iteration++;

                if (iteration % 100 == 0)
                {
                    Console.WriteLine($"iteration: {iteration}, delta: {delta}, FPS: {1000.0f / delta.TotalMilliseconds}, PendingWorkItemCount: {ThreadPool.PendingWorkItemCount}");

                    if (DateTime.Now - start > maxToRun)
                    {
                        return;
                    }
                }

                end = Stopwatch.GetTimestamp();
                delta = new(end - now);

                now = end;
            }
        }
    }
}
