using System;
using SharpVk;

namespace ajiva.Models
{
    public class ManagedImage : IDisposable
    {
        public ImageView View;
        public Image Image;
        public DeviceMemory Memory;

        /// <inheritdoc />
        public void Dispose()
        {
            View.Dispose();
            Image.Dispose();
            Memory.Free();
        }
    }
}
