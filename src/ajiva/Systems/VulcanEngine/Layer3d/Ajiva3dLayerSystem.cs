#region

using ajiva.Components.Media;
using ajiva.Ecs;
using ajiva.Entities;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Layers.Layer3d;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils.Changing;
using SharpVk;
using SharpVk.Glfw;

#endregion

namespace ajiva.Systems.VulcanEngine.Layer3d;

[Dependent(typeof(WindowSystem), typeof(GraphicsSystem))]
public class Ajiva3dLayerSystem : SystemBase, IInit, IUpdate, IAjivaLayer<UniformViewProj3d>
{
    private Format depthFormat;

    private AImage? depthImage;
    private Cameras.Camera? mainCamara;
    private WindowSystem window;

    /// <inheritdoc />
    public Ajiva3dLayerSystem(IAjivaEcs ecs) : base(ecs)
    {
        LayerChanged = new ChangingObserver<IAjivaLayer>(this);
    }

    public Cameras.Camera MainCamara
    {
        get => mainCamara!;
        set
        {
            mainCamara?.Dispose();
            mainCamara = value;
        }
    }

    private object MainLock { get; } = new object();

    public ClearValue[] ClearValues { get; } =
    {
        new ClearColorValue(.1f, .1f, .1f, .1f),
        new ClearDepthStencilValue(1.0f, 0)
    };

    public IAChangeAwareBackupBufferOfT<UniformViewProj3d> LayerUniform { get; set; }

    /// <inheritdoc />
    public IChangingObserver<IAjivaLayer> LayerChanged { get; }

    /// <inheritdoc />
    List<IAjivaLayerRenderSystem> IAjivaLayer.LayerRenderComponentSystems => new List<IAjivaLayerRenderSystem>(LayerRenderComponentSystems);

    /// <inheritdoc />
    public AjivaVulkanPipeline PipelineLayer { get; } = AjivaVulkanPipeline.Pipeline3d;

    /// <inheritdoc />
    public List<IAjivaLayerRenderSystem<UniformViewProj3d>> LayerRenderComponentSystems { get; } = new List<IAjivaLayerRenderSystem<UniformViewProj3d>>();

    /// <inheritdoc />
    public RenderPassLayer CreateRenderPassLayer(SwapChainLayer swapChainLayer, PositionAndMax layerIndex, PositionAndMax layerRenderComponentSystemsIndex)
    {
        var deviceSystem = Ecs.GetSystem<DeviceSystem>();

        if (depthImage is null)
        {
            var imageSystem = Ecs.GetComponentSystem<ImageSystem, AImage>();
            depthFormat = deviceSystem.PhysicalDevice!.FindDepthFormat();
            depthImage = imageSystem.CreateManagedImage(depthFormat, ImageAspectFlags.Depth, swapChainLayer.Canvas);
        }

        var firstPass = layerIndex.First && layerRenderComponentSystemsIndex.First;
        var lastPass = layerIndex.Last && layerRenderComponentSystemsIndex.Last;

        var renderPass = deviceSystem.Device!.CreateRenderPass(new[]
            {
                new AttachmentDescription(AttachmentDescriptionFlags.None,
                    swapChainLayer.SwapChainFormat,
                    SampleCountFlags.SampleCount1,
                    firstPass ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load,
                    AttachmentStoreOp.Store,
                    AttachmentLoadOp.DontCare,
                    AttachmentStoreOp.DontCare,
                    firstPass ? ImageLayout.Undefined : ImageLayout.General,
                    lastPass ? ImageLayout.PresentSource : ImageLayout.General),
                new AttachmentDescription(AttachmentDescriptionFlags.None,
                    depthFormat,
                    SampleCountFlags.SampleCount1,
                    layerRenderComponentSystemsIndex.First ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load,
                    layerRenderComponentSystemsIndex.Last ? AttachmentStoreOp.DontCare : AttachmentStoreOp.Store,
                    AttachmentLoadOp.DontCare,
                    AttachmentStoreOp.DontCare,
                    layerRenderComponentSystemsIndex.First ? ImageLayout.Undefined : ImageLayout.DepthStencilAttachmentOptimal,
                    ImageLayout.DepthStencilAttachmentOptimal)
            },
            new SubpassDescription
            {
                DepthStencilAttachment = new AttachmentReference(1, ImageLayout.DepthStencilAttachmentOptimal),
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachments = new[]
                {
                    new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal)
                }
            },
            new[]
            {
                new SubpassDependency
                {
                    SourceSubpass = Constants.SubpassExternal,
                    DestinationSubpass = 0,
                    SourceStageMask = PipelineStageFlags.BottomOfPipe,
                    SourceAccessMask = AccessFlags.MemoryRead,
                    DestinationStageMask = PipelineStageFlags.ColorAttachmentOutput | PipelineStageFlags.EarlyFragmentTests,
                    DestinationAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite | AccessFlags.DepthStencilAttachmentRead
                },
                new SubpassDependency
                {
                    SourceSubpass = 0,
                    DestinationSubpass = Constants.SubpassExternal,
                    SourceStageMask = PipelineStageFlags.ColorAttachmentOutput | PipelineStageFlags.EarlyFragmentTests,
                    SourceAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite | AccessFlags.DepthStencilAttachmentRead,
                    DestinationStageMask = PipelineStageFlags.BottomOfPipe,
                    DestinationAccessMask = AccessFlags.MemoryRead
                }
            });

        Framebuffer MakeFrameBuffer(ImageView imageView)
        {
            return deviceSystem.Device.CreateFramebuffer(renderPass,
                new[] { imageView, depthImage.View },
                swapChainLayer.Canvas.Width,
                swapChainLayer.Canvas.Height,
                1);
        }

        var frameBuffers = swapChainLayer.SwapChainImages.Select(x => MakeFrameBuffer(x.View!)).ToArray();

        var renderPassLayer = new RenderPassLayer(swapChainLayer, renderPass, frameBuffers, layerRenderComponentSystemsIndex.First ? ClearValues : Array.Empty<ClearValue>());
        swapChainLayer.AddChild(renderPassLayer);
        return renderPassLayer;
    }

    /// <inheritdoc />
    public void Init()
    {
        window = Ecs.GetSystem<WindowSystem>();

        window.OnResize += OnWindowResize;

        window.OnKeyEvent += OnWindowKeyEvent;

        window.OnMouseMove += OnWindowMouseMove;

        var deviceSystem = Ecs.GetSystem<DeviceSystem>();

        LayerUniform = new AChangeAwareBackupBufferOfT<UniformViewProj3d>(1, deviceSystem);

        if (Ecs.TryCreateEntity<Cameras.FpsCamera>(out var mCamTmp))
            MainCamara = mCamTmp;
        else
            ALog.Error("cam not created");
        MainCamara.UpdatePerspective(90, window.Canvas.Width, window.Canvas.Height);
        MainCamara.MovementSpeed = .01f;
    }

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        lock (MainLock)
        {
            UpdateCamaraProjView();
        }
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromMilliseconds(10));

    private void UpdateCamaraProjView()
    {
        var byRef = LayerUniform.GetForChange(0);
        var changed = false;
        if (byRef.Value.View != mainCamara!.View)
        {
            changed = true;
            byRef.Value.View = MainCamara.View;
        }
        if (byRef.Value.Proj != MainCamara.Projection) //todo: we flip the [1,1] value sow it is never the same
        {
            byRef.Value.Proj = MainCamara.Projection;
            byRef.Value.Proj[1, 1] *= -1;

            changed = true;
        }
        if (changed)
            LayerUniform.Commit(0);
    }

    private void OnWindowMouseMove(object? _, AjivaMouseMotionCallbackEventArgs e)
    {
        lock (MainLock)
        {
            var (_, delta, ajivaEngineLayer) = e;
            if (ajivaEngineLayer == AjivaEngineLayer.Layer3d) MainCamara.OnMouseMoved(delta.x, delta.y);
        }
    }

    private void OnWindowKeyEvent(object? _, Key key, int scancode, InputAction action, Modifier modifier)
    {
        lock (MainLock)
        {
            var down = action != InputAction.Release;

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (key)
            {
                case Key.W:
                    MainCamara.Keys.up = down;
                    break;
                case Key.D:
                    MainCamara.Keys.right = down;
                    break;
                case Key.S:
                    MainCamara.Keys.down = down;
                    break;
                case Key.A:
                    MainCamara.Keys.left = down;
                    break;
            }
        }
    }

    private void OnWindowResize()
    {
        lock (MainLock)
        {
            mainCamara?.UpdatePerspective(mainCamara.Fov, window.Canvas.WidthF, window.Canvas.HeightF);
        }
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        depthImage?.Dispose();
        lock (MainLock)
        {
            foreach (var renderSystem in LayerRenderComponentSystems) renderSystem.Dispose();
            LayerUniform.Dispose();
            Ecs.GetSystem<DeviceSystem>().WaitIdle();
            mainCamara?.Dispose();
        }
        base.ReleaseUnmanagedResources(disposing);
    }
}
