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
    };
    public struct UniformViewProj
    {
        public mat4 View;
        public mat4 Proj;
    };
}
