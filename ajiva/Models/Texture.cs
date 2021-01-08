using System;
using SharpVk;

namespace ajiva.Models
{
    public class Texture : IDisposable
    {
        public Sampler Sampler { get; set; } = null!;

        public AImage Image { get; set; } = null!;

        /// <inheritdoc />
        public void Dispose()
        {
            Sampler.Dispose();
            Image.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
