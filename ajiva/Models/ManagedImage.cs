using System;
using SharpVk;

namespace ajiva.Models
{
    public class ManagedImage : IDisposable
    {
        public ImageView View { get; set; } = null!;
        public Image Image { get; set; } = null!;
        public DeviceMemory Memory { get; set; } = null!;

        /// <inheritdoc />
        public void Dispose()
        {
            View.Dispose();
            Image.Dispose();
            Memory.Free();
            GC.SuppressFinalize(this);
        }
    }
}
