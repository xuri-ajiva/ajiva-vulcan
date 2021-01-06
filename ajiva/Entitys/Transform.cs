using GlmSharp;

namespace vulcan_01.Entitys
{
    public struct Transform3d
    {
        public Transform3d(vec3 position, vec3 rotation, vec3 scale)
        {
            this.Position = position;
            this.Rotation = rotation;
            this.Scale = scale;
        }

        public Transform3d(vec3 position, vec3 rotation) : this(position, rotation, Default.Scale) { }
        public Transform3d(vec3 position) : this(position, Default.Rotation) { }

        public vec3 Position;
        public vec3 Rotation;
        public vec3 Scale;
        
        public static readonly Transform3d Default = new(vec3.Zero, vec3.Zero, vec3.Ones);
    }
}
