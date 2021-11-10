using ajiva.Utils.Changing;
using GlmSharp;

namespace ajiva.Components.Transform;

public class Transform2d : DisposingLogger, IComponent, ITransform<vec2, mat4>
{
    private vec2 position;
    private vec2 rotation;
    private vec2 scale;

    public Transform2d(vec2 position, vec2 rotation, vec2 scale)
    {
        ChangingObserver = new ChangingObserverOnlyAfter<ITransform<vec2, mat4>, mat4>(this, () => ModelMat, 0);
        ChangingObserver.RaiseAndSetIfChanged(ref this.position, position);
        ChangingObserver.RaiseAndSetIfChanged(ref this.rotation, rotation);
        ChangingObserver.RaiseAndSetIfChanged(ref this.scale, scale);
    }

    public Transform2d(vec2 position, vec2 rotation) : this(position, rotation, vec2.Ones) { }
    public Transform2d(vec2 position) : this(position, vec2.Zero) { }
    public Transform2d() : this(vec2.Zero) { }

#region propatys

    public vec2 Position
    {
        get => position;
        set => ChangingObserver.RaiseAndSetIfChanged(ref position, value);
    }
    public vec2 Rotation
    {
        get => rotation;
        set => ChangingObserver.RaiseAndSetIfChanged(ref rotation, value);
    }
    public vec2 Scale
    {
        get => scale;
        set => ChangingObserver.RaiseAndSetIfChanged(ref scale, value);
    }

    /// <inheritdoc />
    public IChangingObserverOnlyAfter<ITransform<vec2, mat4>, mat4> ChangingObserver { get; }

    public void RefPosition(ITransform<vec2, mat4>.ModifyRef mod)
    {
        var value = position;
        mod?.Invoke(ref position);
        ChangingObserver.RaiseIfChanged(position, value);
    }

    public void RefRotation(ITransform<vec2, mat4>.ModifyRef mod)
    {
        var value = rotation;
        mod?.Invoke(ref rotation);
        ChangingObserver.RaiseIfChanged(rotation, value);
    }

    public void RefScale(ITransform<vec2, mat4>.ModifyRef mod)
    {
        var value = scale;
        mod?.Invoke(ref scale);
        ChangingObserver.RaiseIfChanged(scale, value);
    }

#endregion

    public mat4 ScaleMat => mat4.Scale(Scale.x, scale.y, 1);
    public mat4 RotationMat => mat4.RotateX(glm.Radians(Rotation.x)) * mat4.RotateY(glm.Radians(Rotation.y));
    public mat4 PositionMat => mat4.Translate(Position.x, position.y, 0);

    public mat4 ModelMat => PositionMat * RotationMat * ScaleMat;

    public override string ToString()
    {
        return $"{nameof(Position)}: {Position}, {nameof(Rotation)}: {Rotation}, {nameof(Scale)}: {Scale}";
    }
}