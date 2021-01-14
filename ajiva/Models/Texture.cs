using System;
using ajiva.Engine;
using SharpVk;

namespace ajiva.Models
{
    public class Texture : DisposingLogger
    {
        public Sampler Sampler { get; set; } = null!;

        public AImage Image { get; set; } = null!;

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            Sampler.Dispose();
            Image.Dispose();
        }
    }
}
