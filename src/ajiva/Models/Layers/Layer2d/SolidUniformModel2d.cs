using ajiva.Utils;
using GlmSharp;

namespace ajiva.Models.Layers.Layer2d
{
    public struct SolidUniformModel2d : IComp<SolidUniformModel2d>
    {
        public mat4 Model;

        /// <inheritdoc />
        public bool CompareTo(SolidUniformModel2d other)
        {
            return Model == other.Model;
        }
    }
}
