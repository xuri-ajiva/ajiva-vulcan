using ajiva.Utils;
using GlmSharp;

namespace ajiva.Models.Layers.Layer3d
{
    public struct SolidUniformModel : IComp<SolidUniformModel>
    {
        public mat4 Model;
        public uint TextureSamplerId;
        public int TextureSamplerId2;
        public int TextureSamplerId3;
        public int TextureSamplerId4;

        /// <inheritdoc />
        public bool CompareTo(SolidUniformModel other)
        {
            return Model == other.Model
                   && TextureSamplerId == other.TextureSamplerId
                   && TextureSamplerId2 == other.TextureSamplerId2
                   && TextureSamplerId3 == other.TextureSamplerId3
                   && TextureSamplerId4 == other.TextureSamplerId4;
        }
    }
}
