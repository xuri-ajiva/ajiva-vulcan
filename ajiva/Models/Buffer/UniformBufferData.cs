using GlmSharp;

namespace ajiva.Models
{
    public struct UniformBufferData
    {
        public mat4 Model;
        public mat4 View;
        public mat4 Proj;
    };
    public struct UniformModel
    {
        public mat4 Model;
        public uint TextureSamplerId;
        public int TextureSamplerId2;
        public int TextureSamplerId3;
        public int TextureSamplerId4;
    };
    public struct UniformViewProj
    {
        public mat4 View;
        public mat4 Proj;
    };
}
