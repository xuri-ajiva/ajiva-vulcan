using System;
using ajiva.Models;
using GlmSharp;

namespace ajiva.Entity
{
    public static class Cameras
    {
        public abstract class Camera : AEntity
        {
            public readonly float Fov;
            public readonly float Width;
            public readonly float Height;

            public bool Moving()
            {
                return Keys.left || Keys.right || Keys.up || Keys.down;
            }

            public class __Keys
            {
                public bool up;
                public bool down;
                public bool right;
                public bool left;
            }

            public readonly __Keys Keys = new();

            public Camera(float fov, float width, float height) : base(Transform3d.Default, new ARenderAble(Mesh.Empty, ARenderAble.DoNotRenderId))
            {
                this.Fov = fov;
                this.Width = width;
                this.Height = height;
                UpdatePerspective(fov, width, height);
                View = mat4.Identity;
            }

            public void Update(in float delta)
            {
                UpdatePosition(delta);
            }

            public abstract void UpdateMatrices();
            public abstract void UpdatePosition(in float delta);
            public abstract void OnMouseMoved(float xRel, float yRel);

            public virtual void Translate(vec3 v)
            {
                Transform.Position += v;
                View += mat4.Translate(v * -1.0F);
            }

            public mat4 Projection { get; protected set; }
            public mat4 View { get; private protected set; }
            public mat4 ProjView => Projection * View;
            public float MovementSpeed { get; set; } = 1;

            public void UpdatePerspective(float fov, float width, float height)
            {
                Projection = mat4.Perspective(fov / 2.0F, width / height, .1F, 1000.0F);
            }
        }
        public sealed class FpsCamera : Camera
        {
            public bool FreeCam { get; set; } = true;
            private vec3 lockAt;
            private const float MouseSensitivity = 0.3F;

            private vec3 CamFront =>
                new vec3(
                    x: -glm.Cos(glm.Radians(Transform.Rotation.x)) * glm.Sin(glm.Radians(Transform.Rotation.y)),
                    y: glm.Sin(glm.Radians(Transform.Rotation.x)),
                    z: glm.Cos(glm.Radians(Transform.Rotation.x)) * glm.Cos(glm.Radians(Transform.Rotation.y))
                ).Normalized;

            public FpsCamera(float fov, float width, float height) : base(fov, width, height)
            {
                OnMouseMoved(0.0f, 0.0f);
            }

            public override void OnMouseMoved(float xRel, float yRel)
            {
                Transform.Rotation.y += xRel * MouseSensitivity; //yaw
                Transform.Rotation.x -= yRel * MouseSensitivity; //pitch
                Transform.Rotation.x = Math.Clamp(Transform.Rotation.x, -89.0F, 89.0f);

                lockAt = CamFront;
                UpdateMatrices();
            }

            public override void UpdateMatrices()
            {
                View = mat4.LookAt(Transform.Position, Transform.Position + lockAt, vec3.UnitY);
            }

            public override void UpdatePosition(in float delta)
            {
                if (!Moving()) return;

                var moveSpeed = delta * MovementSpeed;

                var camFront = CamFront;

                if (Keys.up)
                    Transform.Position += camFront * moveSpeed;
                if (Keys.down)
                    Transform.Position -= camFront * moveSpeed;
                if (Keys.left)
                    Transform.Position -= glm.Cross(camFront, vec3.UnitY).Normalized * moveSpeed;
                if (Keys.right)
                    Transform.Position += glm.Cross(camFront, vec3.UnitY).Normalized * moveSpeed;

                UpdateMatrices();
            }

            public void MoveFront(float amount)
            {
                //								//// not move up and down
                Translate((!FreeCam ? (vec3.UnitX * lockAt).Normalized : lockAt) * amount);

                UpdateMatrices();
            }

            public void MoveSideways(float amount)
            {
                Translate(glm.Cross(lockAt, vec3.UnitY).Normalized * amount);
                UpdateMatrices();
            }

            public void MoveUp(float amount)
            {
                Translate(vec3.UnitY * amount);
                UpdateMatrices();
            }
        };
    }
}
