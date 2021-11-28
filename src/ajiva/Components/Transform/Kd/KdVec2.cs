using GlmSharp;

namespace ajiva.Components.Transform.Kd;

public class KdVec2 : IKdVec
{
    private vec2 value;

    public KdVec2(vec2 value)
    {
        this.value = value;
    }

    /// <inheritdoc />
    public int Dimensions => 2;

    public float this[int dimension]
    {
        get => dimension switch
        {
            0 => value.x,
            1 => value.y,
            _ => 0
        };
        set
        {
            switch (dimension)
            {
                case 0:
                    this.value.x = value;
                    break;
                case 1:
                    this.value.y = value;
                    break;
            }
        }
    }
}
