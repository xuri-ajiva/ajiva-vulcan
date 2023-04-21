using System.Numerics;
using Ajiva.Components.Transform;
using Ajiva.Ecs.Entity.Helper;

namespace Ajiva.Entities;

/*
[EntityComponent(typeof(Transform3d))]
public partial class Camera
{
    public readonly __Keys Keys = new __Keys();
    public float Fov;
    public float Height;
    public float Width;

    protected void InitializeDefault()
    {
        Transform3d ??= new Transform3d();
    }

    public virtual vec3 FrontNormalized { get; }
    public mat4 Projection { get; protected set; }
    public mat4 View { get; private protected set; }
    public mat4 ProjView => Projection * View;
    public float MovementSpeed { get; set; } = 1;

    public bool Moving()
    {
        return Keys.left || Keys.right || Keys.up || Keys.down;
    }

    public virtual void UpdateMatrices()
    {
    }

    public virtual void UpdatePosition(in float delta)
    {
    }

    public virtual void OnMouseMoved(float xRel, float yRel)
    {
    }

    public virtual void Translate(vec3 v)
    {
        this.Transform3d.Position += v;
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
}*/
[EntityComponent(typeof(Transform3d))]
public sealed partial class FpsCamera : IUpdate
{
    private const float MouseSensitivity = 0.3F;
    private Vector3 lockAt;

    public FpsCamera(PeriodicUpdateRunner runner)
    {
        //this.AddComponent(new RenderMesh3D());
        runner.RegisterUpdate(this);
        OnMouseMoved(0.0f, 0.0f);
    }

    public bool FreeCam { get; set; } = true;

    private Vector3 CamFront =>
        Vector3.Normalize(new Vector3(
            -MathF.Cos(Transform3d.Rotation.X.Radians()) * MathF.Sin(Transform3d.Rotation.Y.Radians()),
            MathF.Sin(Transform3d.Rotation.X.Radians()),
            MathF.Cos(Transform3d.Rotation.X.Radians()) * MathF.Cos(Transform3d.Rotation.Y.Radians())
        ));

    //private Transform3d Transform => base.Transform;

    /// <inheritdoc />
    public Vector3 FrontNormalized => CamFront;

    /// <inheritdoc />
    public void Update(UpdateInfo info)
    {
        UpdatePosition((float)info.Delta.TotalMilliseconds);
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromMilliseconds(10));

    public void OnMouseMoved(float xRel, float yRel)
    {
        Transform3d.RefRotation((ref Vector3 r) =>
        {
            r.Y += xRel * MouseSensitivity; //yaw
            r.X -= yRel * MouseSensitivity; //pitch
            r.X = Math.Clamp(Transform3d.Rotation.X, -89.0F, 89.0f);
        });

        lockAt = CamFront;
        UpdateMatrices();
    }

    public void UpdateMatrices()
    {
        View = Matrix4x4.CreateLookAt(Transform3d.Position, Transform3d.Position + lockAt, Vector3.UnitY);
        //View = M(mat4.LookAt(v(Transform3d.Position), v(Transform3d.Position + lockAt), v(Vector3.UnitY)));
    }

    public void UpdatePosition(in float delta)
    {
        if (!Moving()) return;

        var moveSpeed = delta * MovementSpeed;

        var camFront = CamFront;

        if (Keys.up)
            Transform3d.Position += camFront * moveSpeed;
        if (Keys.down)
            Transform3d.Position -= camFront * moveSpeed;
        if (Keys.left)
            Transform3d.Position -= Vector3.Normalize(Vector3.Cross(camFront, Vector3.UnitY)) * moveSpeed;
        if (Keys.right)
            Transform3d.Position += Vector3.Normalize(Vector3.Cross(camFront, Vector3.UnitY)) * moveSpeed;

        UpdateMatrices();
    }

    public void MoveFront(float amount)
    {
        //                      //// not move up and down
        Translate((!FreeCam ? Vector3.Normalize(Vector3.UnitX * lockAt) : lockAt) * amount);

        UpdateMatrices();
    }

    public void MoveSideways(float amount)
    {
        Translate(Vector3.Normalize(Vector3.Cross(lockAt, Vector3.UnitY)) * amount);
        UpdateMatrices();
    }

    public void MoveUp(float amount)
    {
        Translate(Vector3.UnitY * amount);
        UpdateMatrices();
    }

#region camera

    public readonly __Keys Keys = new __Keys();
    public float Fov;
    public float Height;
    public float Width;
    public Matrix4x4 Projection { get; protected set; }
    public Matrix4x4 View { get; private protected set; }
    public Matrix4x4 ProjView => Projection * View;
    public float MovementSpeed { get; set; } = 1;

    public bool Moving()
    {
        return Keys.left || Keys.right || Keys.up || Keys.down;
    }

    public void Translate(Vector3 v)
    {
        Transform3d.Position += v;
        View += Matrix4x4.CreateTranslation(v * -1.0F);
    }

    public void UpdatePerspective(float fov, float width, float height)
    {
        Fov = fov;
        Width = width;
        Height = height;
        //Projection = M(mat4.Perspective(fov / 2.0F, width / height, .1F, 1000.0F));
        Projection = Matrix4x4.CreatePerspectiveFieldOfView((fov / 2.0F).Radians(), width / height, .1F, 1000.0F);
        View = Matrix4x4.Identity;
    }

    /*private Matrix4x4 M(mat4 x)
    {
        return new Matrix4x4(
            x.m00, x.m01, x.m02, x.m03,
            x.m10, x.m11, x.m12, x.m13,
            x.m20, x.m21, x.m22, x.m23,
            x.m30, x.m31, x.m32, x.m33
        );
    }*/

    public class __Keys
    {
        public bool down;
        public bool left;
        public bool right;
        public bool up;
    }

#endregion
}