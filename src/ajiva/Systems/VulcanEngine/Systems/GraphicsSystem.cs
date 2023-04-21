using ajiva.Systems.Assets;
using ajiva.Systems.VulcanEngine.Interfaces;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers;
using ajiva.utils.Changing;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Systems;

public class GraphicsSystem : SystemBase, IUpdate, IGraphicsSystem
{
    private readonly DeviceSystem _deviceSystem;
    private readonly WindowSystem _windowSystem;
    private readonly TextureSystem _textureSystem;
    private static readonly object CurrentGraphicsLayoutSwapLock = new object();

    private AjivaLayerRenderer? ajivaLayerRenderer;


    private bool reInitAjivaLayerRendererNeeded = true;
    private readonly AssetManager _assetManager;

    /// <inheritdoc />
    public GraphicsSystem(DeviceSystem deviceSystem, WindowSystem windowSystem, TextureSystem textureSystem, AssetManager assetManager)
    {
        _deviceSystem = deviceSystem;
        _windowSystem = windowSystem;
        _textureSystem = textureSystem;
        _assetManager = assetManager;

        DepthFormat = (deviceSystem.PhysicalDevice ?? throw new InvalidOperationException()).FindDepthFormat();
        windowSystem.OnResize += WindowResized;
    }

    public IOverTimeChangingObserver ChangingObserver { get; } = new OverTimeChangingObserver(100);

    public List<IAjivaLayer> Layers { get; } = new List<IAjivaLayer>();

    public Format DepthFormat { get; set; }

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
            _deviceSystem.WaitIdle();

            ReCreateRenderUnion();
        }
    }

    private void WindowResized(object sender, Extent2D oldSize, Extent2D newSize)
    {
        RecreateCurrentGraphicsLayout();
    }

    public void DrawFrame()
    {
        var render = _deviceSystem.GraphicsQueue!;
        var presentation = _deviceSystem.PresentQueue!;

        _deviceSystem.ExecuteSingleTimeCommands(QueueType.GraphicsQueue, CommandPoolSelector.Foreground);
        _deviceSystem.ExecuteSingleTimeCommands(QueueType.GraphicsQueue, CommandPoolSelector.Background);
        _deviceSystem.ExecuteSingleTimeCommands(QueueType.TransferQueue, CommandPoolSelector.Transit);

        lock (presentation)
        {
            lock (render)
            {
                ajivaLayerRenderer!.DrawFrame(render, presentation);
            }
        }
    }
    

    protected void ReCreateRenderUnion()
    {
        ajivaLayerRenderer ??= new AjivaLayerRenderer(_deviceSystem, _windowSystem.Canvas, new CommandBufferPool(_deviceSystem), _textureSystem, _assetManager);

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
