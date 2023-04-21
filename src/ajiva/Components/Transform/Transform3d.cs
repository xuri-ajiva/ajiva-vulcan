using ajiva.Utils.Changing;
using GlmSharp;

namespace ajiva.Components.Transform;

public class Transform3d : DisposingLogger, IComponent, ITransform<vec3, mat4>, IModelMatTransform
{
    private vec3 position;
    private vec3 rotation;
    private vec3 scale;

    public Transform3d(vec3 position, vec3 rotation, vec3 scale)
    {
        ChangingObserver = new ChangingObserverOnlyValue<mat4>(() => ModelMat);
        ChangingObserver.RaiseAndSetIfChanged(ref this.position, position);
        ChangingObserver.RaiseAndSetIfChanged(ref this.rotation, rotation);
        ChangingObserver.RaiseAndSetIfChanged(ref this.scale, scale);
    }
    public void UpdateAll()
    {
        ChangingObserver.Changed(ChangingObserver.Result());
    }
    public Transform3d(vec3 position, vec3 rotation) : this(position, rotation, vec3.Ones)
    {
    }

    public Transform3d(vec3 position) : this(position, vec3.Zero)
    {
    }

    public Transform3d() : this(vec3.Zero)
    {
    }

    public mat4 ScaleMat => mat4.Scale(Scale);
    public mat4 RotationMat => mat4.RotateX(glm.Radians(Rotation.x)) * mat4.RotateY(glm.Radians(Rotation.y)) * mat4.RotateZ(glm.Radians(Rotation.z));
    public mat4 PositionMat => mat4.Translate(Position);

    public mat4 ModelMat => PositionMat * RotationMat * ScaleMat;

    public override string ToString()
    {
        return $"{nameof(Position)}: {Position}, {nameof(Rotation)}: {Rotation}, {nameof(Scale)}: {Scale}";
    }

#region propatys

    public vec3 Position
    {
        get => position;
        set => ChangingObserver.RaiseAndSetIfChanged(ref position, value);
    }
    public vec3 Rotation
    {
        get => rotation;
        set => ChangingObserver.RaiseAndSetIfChanged(ref rotation, value);
    }
    public vec3 Scale
    {
        get => scale;
        set => ChangingObserver.RaiseAndSetIfChanged(ref scale, value);
    }

    /// <inheritdoc />
    public IChangingObserverOnlyValue<mat4> ChangingObserver { get; set; }

    public void RefPosition(ITransform<vec3, mat4>.ModifyRef mod)
    {
        var value = position;
        mod?.Invoke(ref position);
        ChangingObserver.RaiseIfChanged(position, value);
    }

    public void RefRotation(ITransform<vec3, mat4>.ModifyRef mod)
    {
        var value = rotation;
        mod?.Invoke(ref rotation);
        ChangingObserver.RaiseIfChanged(rotation, value);
    }

    public void RefScale(ITransform<vec3, mat4>.ModifyRef mod)
    {
        var value = scale;
        mod?.Invoke(ref scale);
        ChangingObserver.RaiseIfChanged(scale, value);
    }

#endregion
}
