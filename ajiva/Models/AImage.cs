using System;
using SharpVk;

namespace ajiva.Models
{
    public class AImage : IDisposable
    {
        private readonly bool disposeImage;
        public ImageView View { get; set; } = null!;
        public Image Image { get; set; } = null!;
        public DeviceMemory? Memory { get; set; } = null!;

        public AImage(bool disposeImage)
        {
            this.disposeImage = disposeImage;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (disposeImage)
                Image.Dispose();
            View.Dispose();
            Memory?.Free();
            GC.SuppressFinalize(this);
        }
    }
}
