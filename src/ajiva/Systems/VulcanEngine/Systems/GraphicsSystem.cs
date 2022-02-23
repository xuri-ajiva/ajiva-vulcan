using ajiva.Ecs;
using ajiva.Systems.VulcanEngine.Interfaces;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers;
using ajiva.Utils.Changing;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Systems;

[Dependent(typeof(TextureSystem))]
public class GraphicsSystem : SystemBase, IInit, IUpdate, IGraphicsSystem
{
    private static readonly object CurrentGraphicsLayoutSwapLock = new object();

    private AjivaLayerRenderer? ajivaLayerRenderer;

    private DeviceSystem deviceSystem;

    private bool reInitAjivaLayerRendererNeeded = true;
    private WindowSystem windowSystem;

    /// <inheritdoc />
    public GraphicsSystem(IAjivaEcs ecs) : base(ecs)
    {
    }

    public IOverTimeChangingObserver ChangingObserver { get; } = new OverTimeChangingObserver(100);

    public List<IAjivaLayer> Layers { get; } = new List<IAjivaLayer>();

    public Format DepthFormat { get; set; }

    /// <inheritdoc />
    public void Init()
    {
        ResolveDeps();
        windowSystem.OnResize += WindowResized;
    }

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        if (reInitAjivaLayerRendererNeeded || ajivaLayerRenderer is null)
        {
            RecreateCurrentGraphicsLayout();
            reInitAjivaLayerRendererNeeded = false;
        }

        if (ChangingObserver.UpdateCycle(delta.Iteration)) UpdateGraphicsData();
        lock (CurrentGraphicsLayoutSwapLock)
        {
            DrawFrame();
        }
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromMilliseconds(10));

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        ajivaLayerRenderer?.Dispose();
        ajivaLayerRenderer = null!;
    }

    public void RecreateCurrentGraphicsLayout()
    {
        lock (CurrentGraphicsLayoutSwapLock)
        {
            deviceSystem.WaitIdle();

            ReCreateRenderUnion();
        }
    }

    private void WindowResized()
    {
        RecreateCurrentGraphicsLayout();
    }

    public void DrawFrame()
    {
        var render = deviceSystem.GraphicsQueue!;
        var presentation = deviceSystem.PresentQueue!;

        deviceSystem.ExecuteSingleTimeCommands(QueueType.GraphicsQueue, CommandPoolSelector.Foreground);
        deviceSystem.ExecuteSingleTimeCommands(QueueType.GraphicsQueue, CommandPoolSelector.Background);
        deviceSystem.ExecuteSingleTimeCommands(QueueType.TransferQueue, CommandPoolSelector.Transit);

        lock (presentation)
        {
            lock (render)
            {
                ajivaLayerRenderer!.DrawFrame(render, presentation);
            }
        }
    }

    public void ResolveDeps()
    {
        deviceSystem = Ecs.Get<DeviceSystem>();
        windowSystem = Ecs.Get<WindowSystem>();

        DepthFormat = (deviceSystem.PhysicalDevice ?? throw new InvalidOperationException()).FindDepthFormat();
    }

    protected void ReCreateRenderUnion()
    {
        ajivaLayerRenderer ??= new AjivaLayerRenderer(deviceSystem, windowSystem.Canvas, new CommandBufferPool(deviceSystem), Ecs);

        ajivaLayerRenderer.Init(Layers);
    }

    public void UpdateGraphicsData()
    {
        ChangingObserver.Updated();
    }

    public void AddUpdateLayer(IAjivaLayer layer)
    {
        layer.LayerChanged.OnChanged += LayerChangedOnOnChanged;
        Layers.Add(layer);
    }

    private void LayerChangedOnOnChanged(IAjivaLayer sender)
    {
        if (!reInitAjivaLayerRendererNeeded)
            reInitAjivaLayerRendererNeeded = true;
    }
}
