using System;
using ajiva.Utils.Changing;
using GlmSharp;

namespace ajiva.Components.Media
{
    public class Transform3d : ChangingComponentBase
    {
        private vec3 position;
        private vec3 rotation;
        private vec3 scale;

        public Transform3d(vec3 position, vec3 rotation, vec3 scale) : base(2)
        {
            ChangingObserver.RaiseChanged(ref this.position, position);
            ChangingObserver.RaiseChanged(ref this.rotation, rotation);
            ChangingObserver.RaiseChanged(ref this.scale, scale);
        }

        public Transform3d(vec3 position, vec3 rotation) : this(position, rotation, Default.Scale) { }
        public Transform3d(vec3 position) : this(position, Default.Rotation) { }

#region propatys

        public vec3 Position
        {
            get => position;
            set => ChangingObserver.RaiseChanged(ref position, value);
        }
        public vec3 Rotation
        {
            get => rotation;
            set => ChangingObserver.RaiseChanged(ref rotation, value);
        }
        public vec3 Scale
        {
            get => scale;
            set => ChangingObserver.RaiseChanged(ref scale, value);
        }

        public void RefPosition(ModifyRef mod)
        {
            var value = position;
            mod?.Invoke(ref position);
            ChangingObserver.RaiseChanged(value, ref position);
        }

        public void RefRotation(ModifyRef mod)
        {
            var value = rotation;
            mod?.Invoke(ref rotation);
            ChangingObserver.RaiseChanged(value, ref rotation);
        }

        public void RefScale(ModifyRef mod)
        {
            var value = scale;
            mod?.Invoke(ref scale);
            ChangingObserver.RaiseChanged(value, ref scale);
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
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
