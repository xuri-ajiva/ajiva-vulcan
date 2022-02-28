using System.Runtime.InteropServices;
using ajiva.Components;
using ajiva.Components.Mesh.Instance;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform.Ui;
using ajiva.Ecs;
using ajiva.Models.Instance;
using ajiva.Models.Layers.Layer2d;
using ajiva.Models.Vertex;
using ajiva.Systems.Assets;
using ajiva.Systems.VulcanEngine.Interfaces;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layer3d;
using ajiva.Systems.VulcanEngine.Layers;
using ajiva.Systems.VulcanEngine.Layers.Creation;
using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Layer2d;

[Dependent(typeof(Ajiva3dLayerSystem))]
public class Mesh2dRenderLayer : ComponentSystemBase<RenderInstanceMesh2D>, IInit, IUpdate, IAjivaLayerRenderSystem<UniformLayer2d>
{
    private readonly object mainLock = new object();
    private InstanceMeshPool<UiInstanceData> instanceMeshPool;
    private long dataVersion;
    private WindowSystem windowSystem;

    /// <inheritdoc />
    public Mesh2dRenderLayer(IAjivaEcs ecs) : base(ecs)
    {
    }

    public PipelineDescriptorInfos[] PipelineDescriptorInfos { get; set; }

    public Shader MainShader { get; set; }

    public IAjivaLayer<UniformLayer2d> AjivaLayer { get; set; }

    /// <inheritdoc />
    public long DataVersion => dataVersion;

    /// <inheritdoc />
    public void DrawComponents(RenderLayerGuard renderGuard, CancellationToken cancellationToken)
    {
        renderGuard.BindDescriptor();

        foreach (var (_, instancedMesh) in instanceMeshPool.InstancedMeshes)
        {
            var dedicatedBufferArray = instanceMeshPool.InstanceMeshData[instancedMesh.InstancedId];

            var instanceBuffer = dedicatedBufferArray.Uniform.Current();
            var vertexBuffer = instancedMesh.Mesh.VertexBuffer;
            var indexBuffer = instancedMesh.Mesh.IndexBuffer;
            renderGuard.Capture(vertexBuffer);
            renderGuard.Capture(indexBuffer);
            renderGuard.Capture(instanceBuffer);
            //renderGuard.Buffer.BindVertexBuffers(VERTEX_BUFFER_BIND_ID, vertexBuffer.Buffer, 0);
            renderGuard.Buffer.BindVertexBuffers(INSTANCE_BUFFER_BIND_ID, instanceBuffer.Buffer, 0);
            //renderGuard.Buffer.BindIndexBuffer(indexBuffer.Buffer, 0, Statics.GetIndexType(indexBuffer.SizeOfT));
            //renderGuard.Buffer.DrawIndexed(6, (uint)dedicatedBufferArray.Length, 0, 0, 0);
            renderGuard.Buffer.Draw(6, (uint)dedicatedBufferArray.Length, 0, 0);
        }
    }

    /// <inheritdoc />
    public void UpdateGraphicsPipelineLayer()
    {
        var bind = new[]
        {
            new VertexInputBindingDescription(INSTANCE_BUFFER_BIND_ID, (uint)Marshal.SizeOf<UiInstanceData>(), VertexInputRate.Instance)
        };

        var attrib = new ViAdBuilder<UiInstanceData>(INSTANCE_BUFFER_BIND_ID)
            .Add(nameof(UiInstanceData.PosCombine), Format.R32G32B32A32SFloat)
            .Add(nameof(UiInstanceData.Rotation), Format.R32G32SFloat)
            .Add(nameof(UiInstanceData.TextureIndex), Format.R32UInt)
            .Add(nameof(UiInstanceData.DrawType), Format.R32UInt)
            .ToArray();

        RenderTarget.GraphicsPipelineLayer = GraphicsPipelineLayerCreator.Default(RenderTarget.PassLayer.Parent, RenderTarget.PassLayer,
            Ecs.Get<DeviceSystem>(), true,
            bind, attrib, MainShader, PipelineDescriptorInfos);
    }

    /// <inheritdoc />
    public RenderTarget RenderTarget { get; set; }

    private const uint VERTEX_BUFFER_BIND_ID = 0;
    private const uint INSTANCE_BUFFER_BIND_ID = 1;


    /// <inheritdoc />
    public void Init()
    {
        var deviceSystem = Ecs.Get<DeviceSystem>();
        windowSystem = Ecs.Get<WindowSystem>();
        windowSystem.OnResize += UiResizeHandler;

        MainShader = Shader.CreateShaderFrom(Ecs.Get<AssetManager>(), "2d", deviceSystem, "main");
        instanceMeshPool = new InstanceMeshPool<UiInstanceData>(deviceSystem);
        instanceMeshPool.Changed.OnChanged += RebuildData;

        var textureSamplerImageViews = Ecs.Get<ITextureSystem>().TextureSamplerImageViews;
        PipelineDescriptorInfos = new[]
        {
            new PipelineDescriptorInfos(DescriptorType.UniformBuffer, ShaderStageFlags.Vertex, 0, 1, BufferInfo: new[]
            {
                new DescriptorBufferInfo { Buffer = AjivaLayer.LayerUniform.Uniform.Buffer!, Offset = 0, Range = (uint)AjivaLayer.LayerUniform.SizeOfT }
            }),
            new(DescriptorType.CombinedImageSampler, ShaderStageFlags.Fragment, 2, (uint)textureSamplerImageViews.Length, ImageInfo: textureSamplerImageViews)
        };
    }

    private void UiResizeHandler(object sender, Extent2D oldSize, Extent2D newSize)
    {
        foreach (var keyValuePair in ComponentEntityMap)
        {
            keyValuePair.Key.Extent = newSize;
        }
        Interlocked.Increment(ref dataVersion);
    }
    private void RebuildData(IInstanceMeshPool<UiInstanceData> sender)
    {
        Interlocked.Increment(ref dataVersion);
    }

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        lock (mainLock)
        {
            instanceMeshPool.CommitInstanceDataChanges();
        }
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromMilliseconds(15));

    /// <inheritdoc />
    public override RenderInstanceMesh2D RegisterComponent(IEntity entity, RenderInstanceMesh2D component)
    {
        if (!entity.TryGetComponent<UiTransform>(out var transform))
            throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

        var res = base.RegisterComponent(entity, component);
        CreateInstance(res);
        component.Extent = windowSystem.Canvas.Extent;
        Interlocked.Increment(ref dataVersion);
        return res;
    }

    private void CreateInstance(RenderInstanceMesh2D res)
    {
        res.Instance = instanceMeshPool.CreateInstance(instanceMeshPool.AsInstanced(res.Mesh));
    }

    private void DeleteInstance(RenderInstanceMesh2D res)
    {
        if (res.Instance == null) return;
        instanceMeshPool.DeleteInstance(res.Instance);
        res.Instance = null;
    }

    /// <inheritdoc />
    public override RenderInstanceMesh2D UnRegisterComponent(IEntity entity, RenderInstanceMesh2D component)
    {
        if (!entity.TryGetComponent<UiTransform>(out var transform))
            throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

        var res = base.UnRegisterComponent(entity, component);
        DeleteInstance(res);
        Interlocked.Increment(ref dataVersion);
        return res;
    }
}
