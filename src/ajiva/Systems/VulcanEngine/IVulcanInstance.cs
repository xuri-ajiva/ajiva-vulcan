using ajiva.Ecs;
using SharpVk;
using SharpVk.Glfw;
using SharpVk.Khronos;

namespace ajiva.Systems.VulcanEngine;

public interface IVulcanInstance : IAjivaEcsObject
{
    PhysicalDevice[] EnumeratePhysicalDevices();
    Surface CreateGlfw3Surface(WindowHandle window);
}
class VulcanInstance : IVulcanInstance
{
    private readonly Instance instance;

    public VulcanInstance(Instance instance)
    {
        this.instance = instance;
    }

    /// <inheritdoc />
    public PhysicalDevice[] EnumeratePhysicalDevices()
    {
        return instance.EnumeratePhysicalDevices();
    }

    /// <inheritdoc />
    public Surface CreateGlfw3Surface(WindowHandle window)
    {
        return instance.CreateGlfw3Surface(window);
    }
}
