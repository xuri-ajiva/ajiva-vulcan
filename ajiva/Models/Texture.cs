using ajiva.Engine;
using SharpVk;

namespace ajiva.Models
{
    public class Texture : DisposingLogger
    {
        public Texture(int textureId)
        {
            TextureId = textureId;
        }

        public int TextureId { get; }
        public Sampler Sampler { get; set; } = null!;

        public AImage Image { get; set; } = null!;

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            Sampler.Dispose();
            Image.Dispose();
        }

        public DescriptorImageInfo DescriptorImageInfo => new() {Sampler = Sampler, ImageView = Image.View, ImageLayout = ImageLayout.ShaderReadOnlyOptimal};
    }
}
