using ajiva.Systems.VulcanEngine;
using ajiva.Utils.Changing;
using SharpVk;

namespace ajiva.Components.Media
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
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            if (disposeImage)
                Image?.Dispose();
            View?.Dispose();
            Memory?.Free();
        }

        /// <inheritdoc />
        public bool Dirty { get; set; }

        public void CreateView(Device device, Format format, ImageAspectFlags aspectFlags)
        {
            view = image?.CreateImageView(device, format, aspectFlags);
        }
    }
}
