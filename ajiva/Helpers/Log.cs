using System;

namespace ajiva.Helpers
{
    public class LogHelper
    {
        public static void Log(object obj)
        {
            lock (WriteLock)
                Console.WriteLine(obj.ToString());
        }

        public static void WriteLine(object obj) => Log(obj);

        public static void Write(string msg, int position)
        {
            lock (WriteLock)
            {
                var (left, top) = Console.GetCursorPosition();

                if (top != position) Console.SetCursorPosition(0, position);

                Console.Write(msg);

                if (top != position) Console.SetCursorPosition(left, top);
            }
        }

        internal static readonly object WriteLock = new();
    }
    public class ConsoleBlock
    {
        public readonly int Count;
        private readonly int top;

        public ConsoleBlock(int count)
        {
            lock (LogHelper.WriteLock)
            {
                this.Count = count;
                top = Console.CursorTop;
                Console.SetCursorPosition(0, top + count);
            }
        }

        public void WriteAt(string msg, int position)
        {
            if (position > Count) throw new ArgumentOutOfRangeException(nameof(position), position, "expected les than count: " + Count);
            LogHelper.Write(msg.FillUp(Console.BufferWidth - 1), top + position);
        }
    }

    public class ConsoleRolBlock
    {
        private ConsoleBlock Block;
        private int index = 0;

        public ConsoleRolBlock(int count)
        {
            Block = new(count);
        }

        public void WriteNext(string msg)
        {
            Block.WriteAt($"[{index:X4}] " + msg, index++ % Block.Count);
        }
    }
}
