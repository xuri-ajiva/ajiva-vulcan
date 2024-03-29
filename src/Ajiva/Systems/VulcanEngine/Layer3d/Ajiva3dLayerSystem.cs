﻿#region

using Ajiva.Components.Media;
using Ajiva.Entities;
using Ajiva.Extensions;
using Ajiva.Models.Buffer.ChangeAware;
using Ajiva.Models.Layers.Layer3d;
using Ajiva.Systems.VulcanEngine.Interfaces;
using Ajiva.Systems.VulcanEngine.Layer;
using Ajiva.Systems.VulcanEngine.Layers.Models;
using Ajiva.Systems.VulcanEngine.Systems;
using SharpVk;
using SharpVk.Glfw;

#endregion

namespace Ajiva.Systems.VulcanEngine.Layer3d;

public class Ajiva3dLayerSystem : SystemBase, IUpdate, IAjivaLayer<UniformViewProj3d>
{
    private readonly EntityFactory _factory;
    private readonly PeriodicUpdateRunner _updateRunner;
    private readonly DeviceSystem deviceSystem;
    private readonly IImageSystem imageSystem;
    private readonly WindowSystem window;
    private Format depthFormat;

    private AImage? depthImage;

    /// <inheritdoc />
    public Ajiva3dLayerSystem(WindowSystem window, IImageSystem imageSystem, DeviceSystem deviceSystem, EntityFactory factory, AjivaConfig config)
    {
        this.window = window;
        this.imageSystem = imageSystem;
        this.deviceSystem = deviceSystem;
        _factory = factory;
        LayerChanged = new ChangingObserver<IAjivaLayer>(this);
        MainCamara = _factory.CreateFpsCamera().Finalize();
        MainCamara.Config = config.CameraConfig;
        MainCamara.UpdatePerspective((float)window.Canvas.Width / window.Canvas.Height);
        this.window.OnResize += OnWindowResize;
        this.window.OnKeyEvent += OnWindowKeyEvent;
        this.window.OnMouseMove += OnWindowMouseMove;
        LayerUniform = new AChangeAwareBackupBufferOfT<UniformViewProj3d>(1, this.deviceSystem);
    }

    public FpsCamera MainCamara { get; private set; }

    private object MainLock { get; } = new object();

    public ClearValue[] ClearValues { get; } = {
        new ClearColorValue(.1f, .1f, .1f, 0), new ClearDepthStencilValue(1.0f, 0)
    };

    /// <inheritdoc />
    public Extent2D Extent { get; } = new Extent2D(2560, 1440);

    public IAChangeAwareBackupBufferOfT<UniformViewProj3d> LayerUniform { get; set; }

    /// <inheritdoc />
    public IChangingObserver<IAjivaLayer> LayerChanged { get; }

    /// <inheritdoc />
    List<IAjivaLayerRenderSystem> IAjivaLayer.LayerRenderComponentSystems => new List<IAjivaLayerRenderSystem>(LayerRenderComponentSystems);

    /// <inheritdoc />
    public List<IAjivaLayerRenderSystem<UniformViewProj3d>> LayerRenderComponentSystems { get; } = new List<IAjivaLayerRenderSystem<UniformViewProj3d>>();

    /// <inheritdoc />
    public RenderTarget CreateRenderPassLayer(SwapChainLayer swapChainLayer, PositionAndMax layerIndex, PositionAndMax layerRenderComponentSystemsIndex)
    {
        if (depthImage is null)
        {
            depthFormat = deviceSystem.PhysicalDevice!.FindDepthFormat();
            depthImage = imageSystem.CreateManagedImage(depthFormat, ImageAspectFlags.Depth, Extent);
        }

        var frameBufferFormat = deviceSystem.PhysicalDevice.FindSupportedFormat(
            new[] {
                Format.R16G16B16A16UNorm, Format.R16G16B16UNorm, Format.R8G8B8UNorm
            },
            ImageTiling.Optimal,
            FormatFeatureFlags.ColorAttachment | FormatFeatureFlags.SampledImage);
        var frameBufferImage = imageSystem.CreateImageAndView(Extent.Width, Extent.Height,
            frameBufferFormat, ImageTiling.Optimal, ImageUsageFlags.ColorAttachment | ImageUsageFlags.Sampled,
            MemoryPropertyFlags.DeviceLocal, ImageAspectFlags.Color);

        var firstPass = layerIndex.First && layerRenderComponentSystemsIndex.First;
        var lastPass = layerIndex.Last && layerRenderComponentSystemsIndex.Last;

        var renderPass = deviceSystem.Device!.CreateRenderPass(new[] {
                new AttachmentDescription(AttachmentDescriptionFlags.None,
                    frameBufferFormat,
                    SampleCountFlags.SampleCount1,
                    AttachmentLoadOp.Clear,
                    AttachmentStoreOp.Store,
                    AttachmentLoadOp.DontCare,
                    AttachmentStoreOp.DontCare,
                    ImageLayout.ColorAttachmentOptimal,
                    ImageLayout.ColorAttachmentOptimal),
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
            new SubpassDescription {
                DepthStencilAttachment = new AttachmentReference(1, ImageLayout.DepthStencilAttachmentOptimal),
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachments = new[] {
                    new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal)
                }
            },
            new[] {
                new SubpassDependency {
                    SourceSubpass = Constants.SubpassExternal,
                    DestinationSubpass = 0,
                    SourceStageMask = PipelineStageFlags.BottomOfPipe,
                    SourceAccessMask = AccessFlags.MemoryRead,
                    DestinationStageMask = PipelineStageFlags.ColorAttachmentOutput | PipelineStageFlags.EarlyFragmentTests,
                    DestinationAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite | AccessFlags.DepthStencilAttachmentRead
                },
                new SubpassDependency {
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
                new[] {
                    imageView, depthImage.View
                },
                Extent.Width,
                Extent.Height,
                1);
        }
        //var frameBuffers = swapChainLayer.SwapChainImages.Select(x => MakeFrameBuffer(x.View)).ToArray();

        var frameBuffer = MakeFrameBuffer(frameBufferImage.View);
        var renderPassLayer = new RenderPassLayer(swapChainLayer, renderPass);
        swapChainLayer.AddChild(renderPassLayer);
        return new RenderTarget {
            ViewPortInfo = new FrameViewPortInfo(frameBuffer, frameBufferImage, Extent, ..1),
            PassLayer = renderPassLayer,
            ClearValues = firstPass
                ? ClearValues
                : new ClearValue[] {
                    new ClearColorValue(.1f, .1f, .1f, 0)
                }
        };
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
        if (byRef.Value.View != MainCamara!.View)
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
            lock (MainLock)
            {
                LayerUniform.Commit(0);
            }
    }

    private void OnWindowMouseMove(object? _, AjivaMouseMotionCallbackEventArgs e)
    {
        lock (MainLock)
        {
            var (_, delta, AjivaEngineLayer) = e;
            if (AjivaEngineLayer == AjivaEngineLayer.Layer3d) MainCamara.OnMouseMoved(delta.X, delta.Y);
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

    private void OnWindowResize(object sender, Extent2D oldSize, Extent2D newSize)
    {
        lock (MainLock)
        {
            MainCamara?.UpdatePerspective((float)newSize.Width / newSize.Height);
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
            deviceSystem.WaitIdle();
            //TODO mainCamara?.Dispose();
        }
        base.ReleaseUnmanagedResources(disposing);
    }
}
