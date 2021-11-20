using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using GlmSharp;

namespace ajiva.Entities;

public static class Cameras
{
    public class FpsCamaraFactory : EntityFactoryBase<FpsCamera>
    {
        /// <inheritdoc />
        public override FpsCamera Create(IAjivaEcs system, uint id)
        {
            var cam = new FpsCamera { Id = id };
            system.TryAttachComponentToEntity(cam, new Transform3d());
            system.TryAttachComponentToEntity(cam, new RenderMesh3D());
            system.RegisterUpdate(cam);
            cam.OnMouseMoved(0.0f, 0.0f);
            return cam;
        }
    }

    public abstract class Camera : TransformFormEntity<Transform3d, vec3, mat4>
    {
        public readonly __Keys Keys = new __Keys();
        public float Fov;
        public float Height;
        public float Width;

        public abstract vec3 FrontNormalized { get; }
        public mat4 Projection { get; protected set; }
        public mat4 View { get; private protected set; }
        public mat4 ProjView => Projection * View;
        public float MovementSpeed { get; set; } = 1;

        public bool Moving()
        {
            return Keys.left || Keys.right || Keys.up || Keys.down;
        }

        public abstract void UpdateMatrices();
        public abstract void UpdatePosition(in float delta);
        public abstract void OnMouseMoved(float xRel, float yRel);

        public virtual void Translate(vec3 v)
        {
            if (this.TryGetComponent<Transform3d>(out var transfrom)) transfrom.Position += v;
            View += mat4.Translate(v * -1.0F);
        }

        public void UpdatePerspective(float fov, float width, float height)
        {
            Fov = fov;
            Width = width;
            Height = height;
            Projection = mat4.Perspective(fov / 2.0F, width / height, .1F, 1000.0F);
            View = mat4.Identity;
        }

        public class __Keys
        {
            public bool down;
            public bool left;
            public bool right;
            public bool up;
        }
    }
    public sealed class FpsCamera : Camera, IUpdate
    {
        private const float MouseSensitivity = 0.3F;
        private vec3 lockAt;
        public bool FreeCam { get; set; } = true;

        private vec3 CamFront =>
            new vec3(
                -glm.Cos(glm.Radians(Transform.Rotation.x)) * glm.Sin(glm.Radians(Transform.Rotation.y)),
                glm.Sin(glm.Radians(Transform.Rotation.x)),
                glm.Cos(glm.Radians(Transform.Rotation.x)) * glm.Cos(glm.Radians(Transform.Rotation.y))
            ).Normalized;

        /// <inheritdoc />
        public override vec3 FrontNormalized => CamFront;

        /// <inheritdoc />
        public void Update(UpdateInfo info)
        {
            UpdatePosition((float)info.Delta.TotalMilliseconds);
        }

        /// <inheritdoc />
        public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromMilliseconds(10));

        public override void OnMouseMoved(float xRel, float yRel)
        {
            Transform.RefRotation((ref vec3 r) =>
            {
                r.y += xRel * MouseSensitivity; //yaw
                r.x -= yRel * MouseSensitivity; //pitch
                r.x = Math.Clamp(Transform.Rotation.x, -89.0F, 89.0f);
            });

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
            //                      //// not move up and down
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
    }
}
