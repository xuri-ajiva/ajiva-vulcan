using ajiva.Ecs;
using ajiva.Models;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Layers.Layer2d;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils.Changing;
using GlmSharp;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Layer2d;

[Dependent(typeof(WindowSystem), typeof(GraphicsSystem))]
public class Ajiva2dLayerSystem : SystemBase, IInit, IUpdate, IAjivaLayer<UniformLayer2d>
{
    private WindowSystem window;

    /// <inheritdoc />
    public Ajiva2dLayerSystem(IAjivaEcs ecs) : base(ecs)
    {
        LayerChanged = new ChangingObserver<IAjivaLayer>(this);
    }

    private object MainLock { get; } = new object();

    /// <inheritdoc />
    public IChangingObserver<IAjivaLayer> LayerChanged { get; }

    /// <inheritdoc />
    List<IAjivaLayerRenderSystem> IAjivaLayer.LayerRenderComponentSystems => new List<IAjivaLayerRenderSystem>(LayerRenderComponentSystems);

    /// <inheritdoc />
    public IAChangeAwareBackupBufferOfT<UniformLayer2d> LayerUniform { get; private set; }

    /// <inheritdoc />
    public List<IAjivaLayerRenderSystem<UniformLayer2d>> LayerRenderComponentSystems { get; } = new List<IAjivaLayerRenderSystem<UniformLayer2d>>();

    /// <inheritdoc />
    public AjivaVulkanPipeline PipelineLayer { get; } = AjivaVulkanPipeline.Pipeline2d;

    /// <inheritdoc />
    public RenderPassLayer CreateRenderPassLayer(SwapChainLayer swapChainLayer, PositionAndMax layerIndex, PositionAndMax layerRenderComponentSystemsIndex)
    {
        var firstPass = layerIndex.First && layerRenderComponentSystemsIndex.First;
        var lastPass = layerIndex.Last && layerRenderComponentSystemsIndex.Last;

        var deviceSystem = Ecs.GetSystem<DeviceSystem>();
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
                    lastPass ? ImageLayout.PresentSource : ImageLayout.General)
            },
            new SubpassDescription
            {
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
                new[]
                {
                    imageView
                },
                swapChainLayer.Canvas.Width,
                swapChainLayer.Canvas.Height,
                1);
        }

        var frameBuffers = swapChainLayer.SwapChainImages.Select(x => MakeFrameBuffer(x.View!)).ToArray();

        var renderPassLayer = new RenderPassLayer(swapChainLayer, renderPass, frameBuffers, firstPass ? new ClearValue[] { new ClearColorValue(.1f, .1f, .1f, .1f) } : Array.Empty<ClearValue>());
        swapChainLayer.AddChild(renderPassLayer);
        return renderPassLayer;
    }

    /// <inheritdoc />
    public void Init()
    {
        window = Ecs.GetSystem<WindowSystem>();

        var canvas = window.Canvas;

        var deviceSystem = Ecs.GetSystem<DeviceSystem>();

        /*MainShader = Shader.CreateShaderFrom("./Shaders/2d", deviceSystem, "main");
        PipelineDescriptorInfos = ajiva.Systems.VulcanEngine.Unions.PipelineDescriptorInfos.CreateFrom(
            LayerUniform.Uniform.Buffer!, (uint)LayerUniform.SizeOfT,
            Models.Uniform.Buffer!, (uint)Models.SizeOfT,
            Ecs.GetComponentSystem<TextureSystem, ATexture>().TextureSamplerImageViews
        );*/

        LayerUniform = new AChangeAwareBackupBufferOfT<UniformLayer2d>(1, deviceSystem);
        const int fac = 50;
        /*LayerUniform.SetAndCommit(0, new UniformLayer2d
            { View = mat4.Translate(-1, -1, 0) * mat4.Scale(2) });*/
        LayerUniform[0] = new UniformLayer2d
            //{ MousePos = new vec2(.5f, .5f), View = mat4.Ortho(-1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f)});           
            {
                Vec2 = new vec2(1337, 421337), MousePos = new vec2(.5f, .5f)
            };
        BuildLayerUniform(window.Canvas);

        window.OnResize += delegate
        {
            BuildLayerUniform(window.Canvas);
        };
        window.OnMouseMove += delegate(object? sender, AjivaMouseMotionCallbackEventArgs args)
        {
            if (args.ActiveLayer == AjivaEngineLayer.Layer2d) MouseMoved(args.Pos);
        };
    }

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
    }

    private void BuildLayerUniform(Canvas canvas)
    {
        BuildLayerUniform(canvas.WidthF, canvas.HeightF);
    }

    private void BuildLayerUniform(float height, float width)
    {
        var byRef = LayerUniform.GetForChange(0);

        byRef.Value.View = mat4.Translate(-width / 2, -height / 2, 0) * mat4.Scale(width * 2, height * 2, 0);
        byRef.Value.Proj = mat4.Ortho(-width, width, -height, height, -1, 1);
        LayerUniform.Commit(0);
    }

    private void MouseMoved(vec2 pos)
    {
        var posNew = new vec2(pos.x / window.Canvas.WidthF, pos.y / window.Canvas.HeightF);

        lock (MainLock)
        {
            LayerUniform[0].Value.MousePos = posNew;
            LayerUniform.Commit(0);
            //LayerUniform.SetAndCommit(0, new UniformLayer2d
            //  { MousePos = posNew });
        }
    }
}