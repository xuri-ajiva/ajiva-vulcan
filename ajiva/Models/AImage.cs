using ajiva.Ecs.Component;
using ajiva.Helpers;
using SharpVk;

namespace ajiva.Models
{
    public class AImage : DisposingLogger, IComponent
    {
        private readonly bool disposeImage;
        private ImageView? view = null!;
        private Image? image = null!;
        private DeviceMemory? memory = null!;
        public ImageView? View
        {
            get => view;
            set
            {
                Dirty = true;
                view = value;
            }
        }
        public Image? Image
        {
            get => image;
            set
            {
                Dirty = true;
                image = value;
            }
        }
        public DeviceMemory? Memory
        {
            get => memory;
            set
            {
                Dirty = true;
                memory = value;
            }
        }

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

        /// <inheritdoc />
        public bool Dirty { get; set; }
    }
}
