using System.Runtime.InteropServices;
using ajiva.Components;
using ajiva.Components.Mesh.Instance;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
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
    private InstanceMeshPool<Mesh2dInstanceData> instanceMeshPool;
    private long dataVersion;

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
            renderGuard.Buffer.BindVertexBuffers(VERTEX_BUFFER_BIND_ID, vertexBuffer.Buffer, 0);
            renderGuard.Buffer.BindVertexBuffers(INSTANCE_BUFFER_BIND_ID, instanceBuffer.Buffer, 0);
            renderGuard.Buffer.BindIndexBuffer(indexBuffer.Buffer, 0, Statics.GetIndexType(indexBuffer.SizeOfT));
            renderGuard.Buffer.DrawIndexed((uint)instancedMesh.Mesh.IndexBuffer.Length, (uint)dedicatedBufferArray.Length, 0, 0, 0);
        }
    }

    /// <inheritdoc />
    public void UpdateGraphicsPipelineLayer()
    {
        var bind = new[]
        {
            Vertex2D.GetBindingDescription(VERTEX_BUFFER_BIND_ID),
            new VertexInputBindingDescription(INSTANCE_BUFFER_BIND_ID, (uint)Marshal.SizeOf<Mesh2dInstanceData>(), VertexInputRate.Instance)
        };

        var attrib = new ViAdBuilder<Mesh2dInstanceData>(Vertex2D.GetAttributeDescriptions(VERTEX_BUFFER_BIND_ID), INSTANCE_BUFFER_BIND_ID)
            .Add(nameof(Mesh2dInstanceData.Position), Format.R32G32SFloat)
            .Add(nameof(Mesh2dInstanceData.Rotation), Format.R32G32SFloat)
            .Add(nameof(Mesh2dInstanceData.Scale), Format.R32G32SFloat)
            .Add(nameof(Mesh2dInstanceData.TextureIndex), Format.R32SInt)
            .Add(nameof(Mesh2dInstanceData.Padding), Format.R32G32SFloat)
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

        MainShader = Shader.CreateShaderFrom(Ecs.Get<AssetManager>(), "2d", deviceSystem, "main");
        instanceMeshPool = new InstanceMeshPool<Mesh2dInstanceData>(deviceSystem);
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

    private void RebuildData(IInstanceMeshPool<Mesh2dInstanceData> sender)
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
        if (!entity.TryGetComponent<Transform2d>(out var transform))
            throw new ArgumentException("Entity needs and transform in order to be rendered as debug");


        var res = base.RegisterComponent(entity, component);
        CreateInstance(res);
        //transform.ChangingObserver.OnChanged += component.OnTransformChange;
        component.TransformChange(transform.ModelMat);
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
        if (!entity.TryGetComponent<Transform2d>(out var transform))
            throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

        //transform.ChangingObserver.OnChanged -= component.OnTransformChange;
        var res = base.UnRegisterComponent(entity, component);
        DeleteInstance(res);
        Interlocked.Increment(ref dataVersion);
        return res;
    }
}
