using System.Runtime.InteropServices;

namespace ajiva.Models
{
    internal static class ImageHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Rgba32
        {
            public byte r;
            public byte g;
            public byte b;
            public byte a;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct Argb32
        {
            public byte b;
            public byte g;
            public byte r;
            public byte a;
        }
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Argb32R
        {
            public readonly byte b;
            public readonly byte g;
            public readonly byte r;
            public readonly byte a;
        }

        public static unsafe void ArgbCopyMap(Argb32R* from, Rgba32* to, uint pixelCount)
        {
            for (var i = 0; i < pixelCount; i++)
            {
                to->r = from->r;
                to->b = from->b;
                to->g = from->g;
                to->a = from->a;

                to++;
                from++;
            }
        }

        public static unsafe void ArgbCopyMap(byte* from, byte* to, uint pixelCount)
        {
            for (var i = 0; i < pixelCount; i++)
            {
                *(to + 0) = *(from + 2);
                *(to + 1) = *(from + 0);
                *(to + 2) = *(from + 1);
                *(to + 3) = *(from + 3);

                to += sizeof(int);
                from += sizeof(int);
            }
        }
    }
}
