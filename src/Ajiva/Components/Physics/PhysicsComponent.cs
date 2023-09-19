using System.Numerics;
using Ajiva.Components.Transform;

namespace Ajiva.Components.Physics;

public class PhysicsComponent : DisposingLogger, IComponent
{
    public Transform3d Transform { get; set; }
    public float Mass { get; set; }
    public Vector3 Position
    {
        get => Transform.Position;
        set => Transform.Position = value;
    }
    public Vector3 Velocity { get; set; }
    public Vector3 Force { get; set; }
    public bool IsStatic { get; set; }

    public float Epsilon { get; set; } = 0.00000001f;
    public float Friction { get; set; } = 0.5f;

    public void Update(TimeSpan deltaTime)
    {
        var d = (float)deltaTime.TotalSeconds;
        if (IsStatic) return;

        //acceleration
        var a = Force / Mass;
        //velocity
        Velocity += a * d;
        //position
        Position += Velocity * d;
        if (Velocity.LengthSquared() < Epsilon) Velocity = Vector3.Zero;
    }

    public void Reset()
    {
        Velocity = new Vector3(0.0f, 0.0f, 0.0f);
        Force = new Vector3(0.0f, 0.0f, 0.0f);
    }

    public void ApplyForce(Vector3 force)
    {
        Force += force;
    }

    //collision response
    public void DoCollisionResponse(PhysicsComponent other)
    {
        if (other.Equals(this)) return;
        if (IsStatic && other.IsStatic) return;
        var normal = Vector3.Normalize(Position - other.Position);
        if (normal == new Vector3(float.NaN)) return;

        var relativeVelocity = Velocity - other.Velocity;
        var relativeVelocityInNormalDirection = Vector3.Dot(relativeVelocity, normal);

        if (relativeVelocityInNormalDirection > 0) return;

        var elasticity = 1.0f;
        var j = -(1.0f + elasticity) * relativeVelocityInNormalDirection;
        j /= 1.0f / Mass + 1.0f / other.Mass;
        var impulse = normal * j;
        if (!IsStatic)
            Velocity += impulse / Mass;
        if (!other.IsStatic)
            other.Velocity -= impulse / other.Mass;
    }
}
/*Shape = new BoxShape(new vec3(size, size, size)),
Mass = 0,
Position = new vec3(0, 0, -100),
Velocity = new vec3(0, 0, 0),
AngularVelocity = new vec3(0, 0, 0),
AngularDamping = 0.1f,
LinearDamping = 0.1f,
Restitution = 0.1f,
Friction = 0.1f,
IsStatic = true*/