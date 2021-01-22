using ajiva.Helpers;
using SharpVk;

namespace ajiva.Models
{
    public class AImage : DisposingLogger
    {
        private readonly bool disposeImage;
        public ImageView? View { get; set; } = null!;
        public Image? Image { get; set; } = null!;
        public DeviceMemory? Memory { get; set; } = null!;

        public AImage(bool disposeImage)
        {
            this.disposeImage = disposeImage;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            if (disposeImage)
                Image?.Dispose();
            View?.Dispose();
            Memory?.Free();
        }
    }
}
