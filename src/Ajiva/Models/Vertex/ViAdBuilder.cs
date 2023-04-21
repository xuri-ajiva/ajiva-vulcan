using System.Runtime.InteropServices;
using SharpVk;

namespace Ajiva.Models.Vertex;

public class ViAdBuilder<T>
{
    private readonly uint bindId;
    private readonly List<VertexInputAttributeDescription> input;
    private int lastLocation;

    public ViAdBuilder(IEnumerable<VertexInputAttributeDescription> input, uint bindId)
    {
        this.input = new List<VertexInputAttributeDescription>(input);
        lastLocation = (int)this.input.Last().Location;
        this.bindId = bindId;
    }

    public ViAdBuilder(uint bindId)
    {
        input = new List<VertexInputAttributeDescription>();
        lastLocation = -1;
        this.bindId = bindId;
    }

    public ViAdBuilder<T> Add(string fieldName, Format format)
    {
        input.Add(For(fieldName, format));
        return this;
    }

    public VertexInputAttributeDescription For(string fieldName, Format format)
    {
        return new VertexInputAttributeDescription((uint)++lastLocation, bindId, format, (uint)Marshal.OffsetOf<T>(fieldName));
    }

    public VertexInputAttributeDescription[] ToArray()
    {
        return input.ToArray();
    }
}