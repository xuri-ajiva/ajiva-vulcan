using ajiva.Ecs.Component;
using GlmSharp;

namespace ajiva.Components
{
    public class Transform3d : IComponent
    {
        public Transform3d(vec3 position, vec3 rotation, vec3 scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public Transform3d(vec3 position, vec3 rotation) : this(position, rotation, Default.Scale) { }
        public Transform3d(vec3 position) : this(position, Default.Rotation) { }

        public vec3 Position;
        public vec3 Rotation;
        public vec3 Scale;

        public static Transform3d Default => new(vec3.Zero, vec3.Zero, vec3.Ones);

        public mat4 ScaleMat => mat4.Scale(Scale);
        public mat4 RotationMat => mat4.RotateX(glm.Radians(Rotation.x)) * mat4.RotateY(glm.Radians(Rotation.y)) * mat4.RotateZ(glm.Radians(Rotation.z));
        public mat4 PositionMat => mat4.Translate(Position);

        public mat4 ModelMat => PositionMat * RotationMat * ScaleMat;

        public override string ToString()
        {
            return $"{nameof(Position)}: {Position}, {nameof(Rotation)}: {Rotation}, {nameof(Scale)}: {Scale}";
        }
    }
}
