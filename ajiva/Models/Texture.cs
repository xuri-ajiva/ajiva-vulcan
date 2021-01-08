using System;
using SharpVk;

namespace ajiva.Models
{
    public class Texture : IDisposable
    {
        public Sampler Sampler;

        public ManagedImage Image;

        /// <inheritdoc />
        public void Dispose()
        {
            Sampler.Dispose();
            Image.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
