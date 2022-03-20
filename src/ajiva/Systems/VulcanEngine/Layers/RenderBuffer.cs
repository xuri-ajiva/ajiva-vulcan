using SharpVk;

namespace ajiva.Systems.VulcanEngine.Layers;

public class RenderBuffer
{
    private bool inUse;

    public RenderBuffer(CommandBuffer commandBuffer, long version)
    {
        CommandBuffer = commandBuffer;
        Version = version;
    }

    public CommandBuffer CommandBuffer { get; set; }
    public long Version { get; set; }

    public List<object> Captured { get; } = new List<object>();
    public bool InUse
    {
        get => inUse;
        set
        {
            ALog.Trace($"Set InUse To: {value,6}, {GetHashCode():X8}");

            inUse = value;
        }
    }
}