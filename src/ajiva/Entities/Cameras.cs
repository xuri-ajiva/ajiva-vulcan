using ajiva.Components.Transform;
using ajiva.Ecs.Entity.Helper;
using GlmSharp;

namespace ajiva.Entities;

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
}

[EntityComponent(typeof(Transform3d))]
public sealed partial class FpsCamera : IUpdate
{
#region camera

    public readonly __Keys Keys = new __Keys();
    public float Fov;
    public float Height;
    public float Width;
    public mat4 Projection { get; protected set; }
    public mat4 View { get; private protected set; }
    public mat4 ProjView => Projection * View;
    public float MovementSpeed { get; set; } = 1;

    public bool Moving()
    {
        return Keys.left || Keys.right || Keys.up || Keys.down;
    }
    

    public void Translate(vec3 v)
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
#endregion
    
    private const float MouseSensitivity = 0.3F;
    private vec3 lockAt;
    public bool FreeCam { get; set; } = true;

    private vec3 CamFront =>
        new vec3(
            -glm.Cos(glm.Radians(Transform3d.Rotation.x)) * glm.Sin(glm.Radians(Transform3d.Rotation.y)),
            glm.Sin(glm.Radians(Transform3d.Rotation.x)),
            glm.Cos(glm.Radians(Transform3d.Rotation.x)) * glm.Cos(glm.Radians(Transform3d.Rotation.y))
        ).Normalized;

    //private Transform3d Transform => base.Transform;

    /// <inheritdoc />
    public vec3 FrontNormalized => CamFront;

    /// <inheritdoc />
    public void Update(UpdateInfo info)
    {
        UpdatePosition((float)info.Delta.TotalMilliseconds);
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromMilliseconds(10));

    public void OnMouseMoved(float xRel, float yRel)
    {
        Transform3d.RefRotation((ref vec3 r) =>
        {
            r.y += xRel * MouseSensitivity; //yaw
            r.x -= yRel * MouseSensitivity; //pitch
            r.x = Math.Clamp(Transform3d.Rotation.x, -89.0F, 89.0f);
        });

        lockAt = CamFront;
        UpdateMatrices();
    }

    public void UpdateMatrices()
    {
        View = mat4.LookAt(Transform3d.Position, Transform3d.Position + lockAt, vec3.UnitY);
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
            Transform3d.Position -= glm.Cross(camFront, vec3.UnitY).Normalized * moveSpeed;
        if (Keys.right)
            Transform3d.Position += glm.Cross(camFront, vec3.UnitY).Normalized * moveSpeed;

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

    public FpsCamera(PeriodicUpdateRunner runner) : base()
    {
        //this.AddComponent(new RenderMesh3D());
        runner.RegisterUpdate(this);
        OnMouseMoved(0.0f, 0.0f);
    }
}
