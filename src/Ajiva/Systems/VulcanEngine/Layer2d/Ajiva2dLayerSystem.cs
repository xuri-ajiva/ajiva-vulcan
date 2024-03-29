﻿using System.Numerics;
using Ajiva.Models.Buffer.ChangeAware;
using Ajiva.Models.Layers.Layer2d;
using Ajiva.Systems.VulcanEngine.Interfaces;
using Ajiva.Systems.VulcanEngine.Layer;
using Ajiva.Systems.VulcanEngine.Layers.Models;
using Ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace Ajiva.Systems.VulcanEngine.Layer2d;

public class Ajiva2dLayerSystem : SystemBase, IAjivaLayer<UniformLayer2d>
{
    private readonly IDeviceSystem _deviceSystem;
    private readonly IImageSystem _imageSystem;
    private readonly WindowSystem _windowSystem;

    /// <inheritdoc />
    public Ajiva2dLayerSystem(IDeviceSystem deviceSystem, WindowSystem windowSystem, IImageSystem imageSystem)
    {
        _windowSystem = windowSystem;
        _imageSystem = imageSystem;
        _deviceSystem = deviceSystem;
        LayerChanged = new ChangingObserver<IAjivaLayer>(this);

        var canvas = _windowSystem.Canvas;

        /*MainShader = Shader.CreateShaderFrom("./Shaders/2d", deviceSystem, "main");
        PipelineDescriptorInfos = Ajiva.Systems.VulcanEngine.Unions.PipelineDescriptorInfos.CreateFrom(
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
                Vec2 = new Vector2(1337, 421337),
                MousePos = new Vector2(.5f, .5f)
            };
        BuildLayerUniform(canvas);

        _windowSystem.OnResize += delegate { BuildLayerUniform(_windowSystem.Canvas); };
        _windowSystem.OnMouseMove += delegate(object? sender, AjivaMouseMotionCallbackEventArgs args)
        {
            if (args.ActiveLayer == AjivaEngineLayer.Layer2d) MouseMoved(args.Pos);
        };
    }

    private object MainLock { get; } = new object();

    /// <inheritdoc />
    public Extent2D Extent { get; } = new Extent2D(2560, 1440);

    /// <inheritdoc />
    public IChangingObserver<IAjivaLayer> LayerChanged { get; }

    /// <inheritdoc />
    List<IAjivaLayerRenderSystem> IAjivaLayer.LayerRenderComponentSystems => new List<IAjivaLayerRenderSystem>(LayerRenderComponentSystems);

    /// <inheritdoc />
    public IAChangeAwareBackupBufferOfT<UniformLayer2d> LayerUniform { get; }

    /// <inheritdoc />
    public List<IAjivaLayerRenderSystem<UniformLayer2d>> LayerRenderComponentSystems { get; } = new List<IAjivaLayerRenderSystem<UniformLayer2d>>();

    /// <inheritdoc />
    public RenderTarget CreateRenderPassLayer(SwapChainLayer swapChainLayer, PositionAndMax layerIndex, PositionAndMax layerRenderComponentSystemsIndex)
    {
        var frameBufferFormat = _deviceSystem.PhysicalDevice.FindSupportedFormat(
            new[] {
                Format.R16G16B16A16UNorm, Format.R16G16B16UNorm, Format.R8G8B8UNorm
            },
            ImageTiling.Optimal,
            FormatFeatureFlags.ColorAttachment | FormatFeatureFlags.SampledImage | FormatFeatureFlags.SampledImageFilterLinear);
        var frameBufferImage = _imageSystem.CreateImageAndView(Extent.Width, Extent.Height,
            frameBufferFormat, ImageTiling.Optimal, ImageUsageFlags.ColorAttachment | ImageUsageFlags.Sampled,
            MemoryPropertyFlags.DeviceLocal, ImageAspectFlags.Color);

        var renderPass = _deviceSystem.Device!.CreateRenderPass(new[] {
                new AttachmentDescription(AttachmentDescriptionFlags.None,
                    frameBufferFormat,
                    SampleCountFlags.SampleCount1,
                    AttachmentLoadOp.Clear,
                    AttachmentStoreOp.Store,
                    AttachmentLoadOp.DontCare,
                    AttachmentStoreOp.DontCare,
                    ImageLayout.ColorAttachmentOptimal,
                    ImageLayout.ColorAttachmentOptimal)
            },
            new SubpassDescription {
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

        var frameBuffer = _deviceSystem.Device.CreateFramebuffer(renderPass, new[] {
            frameBufferImage.View
        }, Extent.Width, Extent.Height, 1);

        var renderPassLayer = new RenderPassLayer(swapChainLayer, renderPass);
        swapChainLayer.AddChild(renderPassLayer);
        return new RenderTarget {
            ViewPortInfo = new FrameViewPortInfo(frameBuffer, frameBufferImage, Extent, ..1),
            PassLayer = renderPassLayer,
            ClearValues = new ClearValue[] {
                new ClearColorValue(.1f, .1f, .1f, .1f)
            }
        };
    }

    private void BuildLayerUniform(Canvas canvas)
    {
        BuildLayerUniform(canvas.WidthF, canvas.HeightF);
    }

    private void BuildLayerUniform(float height, float width)
    {
        var byRef = LayerUniform.GetForChange(0);

        byRef.Value.View = Matrix4x4.CreateTranslation(-width / 2, -height / 2, 0) * Matrix4x4.CreateScale(width * 2, height * 2, 0);
        //TODO OLD: byRef.Value.Proj = mat4.Ortho(-width, width, -height, height, -1, 1);
        byRef.Value.Proj = Matrix4x4.CreateOrthographic(width, height, -1, 1);
        LayerUniform.Commit(0);
    }

    private void MouseMoved(Vector2 pos)
    {
        var posNew = new Vector2(pos.X / _windowSystem.Canvas.WidthF, pos.Y / _windowSystem.Canvas.HeightF);

        lock (MainLock)
        {
            LayerUniform[0].Value.MousePos = posNew;
            LayerUniform.Commit(0);
            //LayerUniform.SetAndCommit(0, new UniformLayer2d
            //  { MousePos = posNew });
        }
    }
}