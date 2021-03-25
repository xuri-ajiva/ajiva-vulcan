using System;
using ajiva.Ecs.Component;
using GlmSharp;

namespace ajiva.Components.Media
{
    public class Transform3d : IComponent
    {
        private vec3 position;
        private vec3 rotation;
        private vec3 scale;

        public Transform3d(vec3 position, vec3 rotation, vec3 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public Transform3d(vec3 position, vec3 rotation) : this(position, rotation, Default.Scale) { }
        public Transform3d(vec3 position) : this(position, Default.Rotation) { }

#region propatys

        public vec3 Position
        {
            get => position;
            set
            {
                Dirty = true;
                position = value;
            }
        }
        public vec3 Rotation
        {
            get => rotation;
            set
            {
                Dirty = true;
                rotation = value;
            }
        }
        public vec3 Scale
        {
            get => scale;
            set
            {
                Dirty = true;
                scale = value;
            }
        }

        public void RefPosition(ModifyRef mod)
        {
            mod?.Invoke(ref position);
            Dirty = true;
        }

        public void RefRotation(ModifyRef mod)
        {
            mod?.Invoke(ref rotation);
            Dirty = true;
        }

        public void RefScale(ModifyRef mod)
        {
            mod?.Invoke(ref scale);
            Dirty = true;
        }

        public delegate void ModifyRef(ref vec3 vec);

  #endregion

        public static Transform3d Default => new(vec3.Zero, vec3.Zero, vec3.Ones);

        public mat4 ScaleMat => mat4.Scale(Scale);
        public mat4 RotationMat => mat4.RotateX(glm.Radians(Rotation.x)) * mat4.RotateY(glm.Radians(Rotation.y)) * mat4.RotateZ(glm.Radians(Rotation.z));
        public mat4 PositionMat => mat4.Translate(Position);

        public mat4 ModelMat => PositionMat * RotationMat * ScaleMat;

        public override string ToString()
        {
            return $"{nameof(Position)}: {Position}, {nameof(Rotation)}: {Rotation}, {nameof(Scale)}: {Scale}";
        }

        /// <inheritdoc />
        public bool Dirty { get; set; }

        /// <inheritdoc />
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
