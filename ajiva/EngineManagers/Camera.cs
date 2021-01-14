using System;
using ajiva.Entitys;
using ajiva.Models;
using GlmSharp;

namespace ajiva.EngineManagers
{
    public static class Cameras
    {
        public abstract class Camera : AEntity
        {
            public bool Moving()
            {
                return keys.left || keys.right || keys.up || keys.down;
            }

            public class __Keys
            {
                public bool up;
                public bool down;
                public bool right;
                public bool left;
            }

            public __Keys keys = new();

            public Camera(float fov, float width, float height) : base(Transform3d.Default, Mesh.Empty)
            {
                Projection = mat4.Perspective(fov / 2.0F, width / height, .1F, 1000.0F);
                View = mat4.Identity;
            }

            public void Update(in float delta)
            {
                UpdatePosition(delta);
                UpdateMatrices();
            }

            public virtual void UpdateMatrices() { ViewProj = Projection * View; }
            public virtual void UpdatePosition(in float delta) { }

            public virtual void Translate(vec3 v)
            {
                Transform.Position += v;
                View += mat4.Translate(v * -1.0F);
            }

            public mat4 Projection { get; protected set; }
            public mat4 View { get; private protected set; }
            public mat4 ViewProj { get; private protected set; }
            public float MovementSpeed { get; set; } = 1;
        }
        public class FpsCamera : Camera
        {
            public bool FreeCam { get; set; } = true;
            protected vec3 LockAt;
            protected const float MouseSensitivity = 0.3F;

            private vec3 CamFront =>
                new vec3(
                    x: -glm.Cos(glm.Radians(Transform.Rotation.x)) * glm.Sin(glm.Radians(Transform.Rotation.y)),
                    y: glm.Sin(glm.Radians(Transform.Rotation.x)),
                    z: glm.Cos(glm.Radians(Transform.Rotation.x)) * glm.Cos(glm.Radians(Transform.Rotation.y))
                ).Normalized;

            public FpsCamera(float fov, float width, float height) : base(fov, width, height)
            {
                OnMouseMoved(0.0f, 0.0f);
                base.UpdateMatrices();
            }

            public void OnMouseMoved(float xRel, float yRel)
            {
                Transform.Rotation.y += xRel * MouseSensitivity; //yaw
                Transform.Rotation.x -= yRel * MouseSensitivity; //pitch
                Transform.Rotation.x = Math.Clamp(Transform.Rotation.x, -89.0F, 89.0f);

                LockAt = CamFront;
                UpdateMatrices();
            }

            public override void UpdateMatrices()
            {
                View = mat4.LookAt(Transform.Position, Transform.Position + LockAt, vec3.UnitY);
                base.UpdateMatrices();
            }

            public override void UpdatePosition(in float delta)
            {
                if (!Moving()) return;

                var moveSpeed = delta * MovementSpeed;

                var camFront = CamFront;

                if (keys.up)
                    Transform.Position += camFront * moveSpeed;
                if (keys.down)
                    Transform.Position -= camFront * moveSpeed;
                if (keys.left)
                    Transform.Position -= glm.Cross(camFront, vec3.UnitY).Normalized * moveSpeed;
                if (keys.right)
                    Transform. Position += glm.Cross(camFront, vec3.UnitY).Normalized * moveSpeed;
            }

            public void MoveFront(float amount)
            {
                //								//// not move up and down
                Translate((!FreeCam ? ((vec3.UnitX * LockAt).Normalized) : LockAt) * amount);

                UpdateMatrices();
            }

            public void MoveSideways(float amount)
            {
                Translate(glm.Cross(LockAt, vec3.UnitY).Normalized * amount);
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
