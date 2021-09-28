using System;
using SharpVk;

namespace ajiva.Models.Buffer
{
    public interface IBufferOfT
    {
        SharpVk.Buffer? Buffer {get;}
        DeviceMemory? Memory {get;}
        int Length { get; }
        uint SizeOfT { get; }
        uint Size { get; }
        IntPtr Map();
        void Unmap();
        ABuffer.DisposablePointer MapDisposer();
    }
}