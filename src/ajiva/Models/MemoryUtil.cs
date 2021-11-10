using System.Runtime.InteropServices;

namespace ajiva.Models;

public static class MemoryUtil
{
    [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
    public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

    [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
    public static extern void Copy(IntPtr dest, IntPtr src, uint count);
}