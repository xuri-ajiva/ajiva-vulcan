using GlmSharp;

namespace ajiva.Models
{
    public struct UniformBufferData : IComp<UniformBufferData>
    {
        public mat4 Model;
        public mat4 View;
        public mat4 Proj;

        /// <inheritdoc />
        public bool CompareTo(UniformBufferData other)
        {
            return View == other.View && Proj == other.Proj && Model == other.Model;
        }
    };
    public struct UniformModel : IComp<UniformModel>
    {
        public mat4 Model;
        public uint TextureSamplerId;
        public int TextureSamplerId2;
        public int TextureSamplerId3;
        public int TextureSamplerId4;

        /// <inheritdoc />
        public bool CompareTo(UniformModel other)
        {
            return Model == other.Model
                   && TextureSamplerId == other.TextureSamplerId
                   && TextureSamplerId2 == other.TextureSamplerId2
                   && TextureSamplerId3 == other.TextureSamplerId3
                   && TextureSamplerId4 == other.TextureSamplerId4;
        }
    };
    public struct UniformViewProj : IComp<UniformViewProj>
    {
        public mat4 View;
        public mat4 Proj;

        /// <inheritdoc />
        public bool CompareTo(UniformViewProj other)
        {
            return View == other.View && Proj == other.Proj;
        }
    };
    public interface IComp<in T>
    {
        public bool CompareTo(T other);
    }
}
