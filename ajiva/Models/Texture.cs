using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using ajiva.Engine;
using ajiva.EngineManagers;
using SharpVk;

namespace ajiva.Models
{
    public partial class Texture : DisposingLogger
    {
        private static int currentMaxId = 0;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static int NextId() => currentMaxId++;

        public Texture()
        {
            TextureId = NextId();
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
