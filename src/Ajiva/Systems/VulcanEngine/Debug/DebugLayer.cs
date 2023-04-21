using System.Numerics;
using System.Runtime.InteropServices;
using Ajiva.Components;
using Ajiva.Components.Mesh;
using Ajiva.Components.Mesh.Instance;
using Ajiva.Components.RenderAble;
using Ajiva.Components.Transform;
using Ajiva.Models.Instance;
using Ajiva.Models.Layers.Layer3d;
using Ajiva.Models.Vertex;
using Ajiva.Systems.Assets;
using Ajiva.Systems.VulcanEngine.Interfaces;
using Ajiva.Systems.VulcanEngine.Layer;
using Ajiva.Systems.VulcanEngine.Layers;
using SharpVk;

namespace Ajiva.Systems.VulcanEngine.Debug;

public class DebugLayer : ComponentSystemBase<DebugComponent>, IUpdate, IAjivaLayerRenderSystem<UniformViewProj3d>
{
    private readonly IDeviceSystem _deviceSystem;
    private readonly IAssetManager _assetManager;
    private readonly ITextureSystem _textureSystem;
    private InstanceMeshPool<MeshInstanceData> instanceMeshPool;
    private readonly object mainLock = new object();
    private long dataVersion;

    /// <inheritdoc />
    /// <inheritdoc />
    public DebugLayer(IDeviceSystem deviceSystem, IAssetManager assetManager, ITextureSystem textureSystem)
    {
        _deviceSystem = deviceSystem;
        _assetManager = assetManager;
        _textureSystem = textureSystem;
        GraphicsDataChanged = new ChangingObserver<IAjivaLayerRenderSystem>(this);
        MainShader = Shader.CreateShaderFrom(assetManager, "3d/debug", deviceSystem, "main");
        instanceMeshPool = new InstanceMeshPool<MeshInstanceData>(deviceSystem);
        instanceMeshPool.Changed.OnChanged += RebuildData;
    }

    public PipelineDescriptorInfos[]? PipelineDescriptorInfos { get; set; }

    public Shader MainShader { get; set; }

    /// <inheritdoc />
    public IChangingObserver<IAjivaLayerRenderSystem> GraphicsDataChanged { get; }

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
        var bind = new[] {
            Vertex3D.GetBindingDescription(VERTEX_BUFFER_BIND_ID), new VertexInputBindingDescription(INSTANCE_BUFFER_BIND_ID, (uint)Marshal.SizeOf<MeshInstanceData>(), VertexInputRate.Instance)
        };

        var attrib = new ViAdBuilder<MeshInstanceData>(Vertex3D.GetAttributeDescriptions(VERTEX_BUFFER_BIND_ID), INSTANCE_BUFFER_BIND_ID)
            .Add(nameof(MeshInstanceData.Position), Format.R32G32B32SFloat)
            .Add(nameof(MeshInstanceData.Rotation), Format.R32G32B32SFloat)
            .Add(nameof(MeshInstanceData.Scale), Format.R32G32B32SFloat)
            .Add(nameof(MeshInstanceData.TextureIndex), Format.R32SInt)
            .Add(nameof(MeshInstanceData.Padding), Format.R32G32SFloat)
            .ToArray();
        if (PipelineDescriptorInfos is null)
            CreatePipelineDescriptorInfos();

        RenderTarget.GraphicsPipelineLayer = CreateDebugPipe.Default(RenderTarget.PassLayer.Parent, RenderTarget.PassLayer,
            _deviceSystem, true,
            bind, attrib, MainShader, PipelineDescriptorInfos);
    }

    private void CreatePipelineDescriptorInfos()
    {
        var textureSamplerImageViews = _textureSystem.TextureSamplerImageViews;
        PipelineDescriptorInfos = new[] {
            new PipelineDescriptorInfos(DescriptorType.UniformBuffer, ShaderStageFlags.Vertex, 0, 1, BufferInfo: new[] {
                new DescriptorBufferInfo {
                    Buffer = AjivaLayer.LayerUniform.Uniform.Buffer!,
                    Offset = 0,
                    Range = (uint)AjivaLayer.LayerUniform.SizeOfT
                }
            }),
            new(DescriptorType.CombinedImageSampler, ShaderStageFlags.Fragment, 2, (uint)textureSamplerImageViews.Length, ImageInfo: textureSamplerImageViews)
        };
    }

    /// <inheritdoc />
    public RenderTarget RenderTarget { get; set; }

    private const uint VERTEX_BUFFER_BIND_ID = 0;
    private const uint INSTANCE_BUFFER_BIND_ID = 1;

    /// <inheritdoc />
    public IAjivaLayer<UniformViewProj3d> AjivaLayer { get; set; }

    private void RebuildData(IInstanceMeshPool<MeshInstanceData> sender)
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
    public override DebugComponent RegisterComponent(IEntity entity, DebugComponent component)
    {
        if (!entity.TryGetComponent<Transform3d>(out var transform))
            throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

        transform.ChangingObserver.OnChanged += component.OnTransformChange;
        component.TransformChange(transform.ModelMat);

        var res = base.RegisterComponent(entity, component);
        CreateInstance(res);
        Interlocked.Increment(ref dataVersion);
        return res;
    }

    /// <inheritdoc />
    public override DebugComponent UnRegisterComponent(IEntity entity, DebugComponent component)
    {
        if (!entity.TryGetComponent<Transform3d>(out var transform))
            throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

        transform.ChangingObserver.OnChanged -= component.OnTransformChange;

        var res = base.UnRegisterComponent(entity, component);
        DeleteInstance(res);
        Interlocked.Increment(ref dataVersion);
        return res;
    }

    public override DebugComponent CreateComponent(IEntity entity)
    {
        if (!entity.TryGetComponent<Transform3d>(out var transform))
            transform = new Transform3d();

        return new DebugComponent(entity.TryGetComponent<RenderInstanceMesh>(out var meshInstance)
                ? meshInstance.Mesh
                : MeshPrefab.Cube,
            transform);
    }

    private void CreateInstance(DebugComponent res)
    {
        res.Instance = instanceMeshPool.CreateInstance(instanceMeshPool.AsInstanced(res.Mesh));
    }

    private void DeleteInstance(DebugComponent res)
    {
        if (res.Instance == null) return;
        instanceMeshPool.DeleteInstance(res.Instance);
        res.Instance = null;
    }
}
public class DebugComponent : RenderMeshIdUnique<DebugComponent>
{
    private readonly Transform3d transform;
    public IInstancedMeshInstance<MeshInstanceData>? Instance { get; set; }

    public DebugComponent(IMesh mesh, Transform3d transform)
    {
        this.transform = transform;
        transform.ChangingObserver.OnChanged += TransformChange;
        Mesh = mesh;
        OnTransformChange = TransformChange;
    }

    public IMesh Mesh { get; }

    public void TransformChange(Matrix4x4 value)
    {
        Instance?.UpdateData(Update);
    }

    private void Update(ref MeshInstanceData value)
    {
        value.Position = transform.Position;
        value.Rotation = transform.Rotation; //todo: fix radians?
        value.Scale = transform.Scale;
        value.Padding = Vector2.One;;
    }

    public IChangingObserverOnlyValue<Matrix4x4>.OnChangedDelegate OnTransformChange { get; }

    public bool DrawTransform { get; set; }
    public bool DrawWireframe { get; set; }
    public bool NoDepthTest { get; set; }
}
