﻿using System.Runtime.InteropServices;
using SharpVk;

namespace ajiva.Models.Vertex;

public class ViAdBuilder<T>
{
    private readonly List<VertexInputAttributeDescription> input;
    private uint bindId;
    private int lastLocation;

    public ViAdBuilder(IEnumerable<VertexInputAttributeDescription> input, uint bindId)
    {
        this.input = new List<VertexInputAttributeDescription>(input);
        lastLocation = (int)this.input.Last().Location;
        this.bindId = bindId;
    }

    public ViAdBuilder(uint bindId)
    {
        this.input = new List<VertexInputAttributeDescription>();
        lastLocation = -1;
        this.bindId = bindId;
    }

    public ViAdBuilder<T> Add(string fieldName, Format format)
    {
        input.Add(For(fieldName, format));
        return this;
    }

    public VertexInputAttributeDescription For(string fieldName, Format format) => new VertexInputAttributeDescription((uint)++lastLocation, bindId, format, (uint)Marshal.OffsetOf<T>(fieldName));

    public VertexInputAttributeDescription[] ToArray() => input.ToArray();
}
