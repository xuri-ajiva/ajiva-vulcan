﻿using System.Runtime.InteropServices;
using Ajiva.Assets;
using Ajiva.Components;
using Ajiva.Components.Media;
using Ajiva.Components.Mesh;
using Ajiva.Components.Mesh.Instance;
using Ajiva.Components.RenderAble;
using Ajiva.Components.Transform.Ui;
using Ajiva.Models.Instance;
using Ajiva.Models.Layers.Layer2d;
using Ajiva.Models.Vertex;
using Ajiva.Systems.VulcanEngine.Interfaces;
using Ajiva.Systems.VulcanEngine.Layer;
using Ajiva.Systems.VulcanEngine.Layers;
using Ajiva.Systems.VulcanEngine.Layers.Creation;
using Ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace Ajiva.Systems.VulcanEngine.Layer2d;

public class Mesh2dRenderLayer : ComponentSystemBase<RenderInstanceMesh2D>, IUpdate, IAjivaLayerRenderSystem<UniformLayer2d>
{
    private const uint VERTEX_BUFFER_BIND_ID = 0;
    private const uint INSTANCE_BUFFER_BIND_ID = 1;
    private readonly IAssetManager _assetManager;
    private readonly DeviceSystem _deviceSystem;
    private readonly InstanceMeshPool<UiInstanceData> _instanceMeshPool;
    private readonly object _mainLock = new object();
    private readonly ITextureSystem _textureSystem;
    private readonly WindowSystem _windowSystem;
    private long _dataVersion;

    /// <inheritdoc />
    public Mesh2dRenderLayer(WindowSystem windowSystem, DeviceSystem deviceSystem, IAssetManager assetManager, ITextureSystem textureSystem)
    {
        _windowSystem = windowSystem;
        _deviceSystem = deviceSystem;
        _assetManager = assetManager;
        _textureSystem = textureSystem;

        _windowSystem.OnResize += UiResizeHandler;

        MainShader = Shader.CreateShaderFrom(_assetManager, "2d", _deviceSystem, "main");
        _instanceMeshPool = new InstanceMeshPool<UiInstanceData>(_deviceSystem);
        _instanceMeshPool.Changed.OnChanged += RebuildData;
    }

    public PipelineDescriptorInfos[]? PipelineDescriptorInfos { get; set; }

    public Shader MainShader { get; set; }

    public IAjivaLayer<UniformLayer2d> AjivaLayer { get; set; }

    /// <inheritdoc />
    public long DataVersion => _dataVersion;

    /// <inheritdoc />
    public void DrawComponents(RenderLayerGuard renderGuard, CancellationToken cancellationToken)
    {
        renderGuard.BindDescriptor();

        foreach (var (_, instancedMesh) in _instanceMeshPool.InstancedMeshes)
        {
            var dedicatedBufferArray = _instanceMeshPool.InstanceMeshData[instancedMesh.InstancedId];

            var instanceBuffer = dedicatedBufferArray.Uniform.Current();
            var vertexBuffer = instancedMesh.Mesh.VertexBuffer;
            var indexBuffer = instancedMesh.Mesh.IndexBuffer;
            renderGuard.Capture(vertexBuffer);
            renderGuard.Capture(indexBuffer);
            renderGuard.Capture(instanceBuffer);
            renderGuard.Buffer.BindVertexBuffers(VERTEX_BUFFER_BIND_ID, vertexBuffer.Buffer, 0);
            renderGuard.Buffer.BindVertexBuffers(INSTANCE_BUFFER_BIND_ID, instanceBuffer.Buffer, 0);
            renderGuard.Buffer.BindIndexBuffer(indexBuffer.Buffer, 0, Statics.GetIndexType(indexBuffer.SizeOfT));
            renderGuard.Buffer.DrawIndexed(6, (uint)dedicatedBufferArray.Length, 0, 0, 0);
        }
    }

    /// <inheritdoc />
    public void UpdateGraphicsPipelineLayer()
    {
        var bind = new[] {
            Vertex2D.GetBindingDescription(VERTEX_BUFFER_BIND_ID), new VertexInputBindingDescription(INSTANCE_BUFFER_BIND_ID, (uint)Marshal.SizeOf<UiInstanceData>(), VertexInputRate.Instance)
        };

        var attrib = new ViAdBuilder<UiInstanceData>(
                Vertex2D.GetAttributeDescriptions(VERTEX_BUFFER_BIND_ID), INSTANCE_BUFFER_BIND_ID)
            .Add(nameof(UiInstanceData.Offset), Format.R32G32SFloat)
            .Add(nameof(UiInstanceData.Scale), Format.R32G32SFloat)
            .Add(nameof(UiInstanceData.Rotation), Format.R32G32SFloat)
            .Add(nameof(UiInstanceData.TextureIndex), Format.R32UInt)
            .Add(nameof(UiInstanceData.DrawType), Format.R32UInt)
            .ToArray();

        if (PipelineDescriptorInfos is null)
            CreatePipelineDescriptorInfos();

        RenderTarget.GraphicsPipelineLayer = GraphicsPipelineLayerCreator.Default(RenderTarget.PassLayer.Parent, RenderTarget.PassLayer,
            _deviceSystem, true,
            bind, attrib, MainShader, PipelineDescriptorInfos);
    }

    /// <inheritdoc />
    public RenderTarget RenderTarget { get; set; }

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        lock (_mainLock)
        {
            _instanceMeshPool.CommitInstanceDataChanges();
        }
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromMilliseconds(15));

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

    private void UiResizeHandler(object sender, Extent2D oldSize, Extent2D newSize)
    {
        Interlocked.Increment(ref _dataVersion);
    }

    private void RebuildData(IInstanceMeshPool<UiInstanceData> sender)
    {
        Interlocked.Increment(ref _dataVersion);
    }

    /// <inheritdoc />
    public override RenderInstanceMesh2D RegisterComponent(IEntity entity, RenderInstanceMesh2D component)
    {
        if (!entity.TryGetComponent<UiTransform>(out var transform))
            throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

        var res = base.RegisterComponent(entity, component);
        CreateInstance(res);
        component.UpdateData();
        Interlocked.Increment(ref _dataVersion);
        return res;
    }

    private void CreateInstance(RenderInstanceMesh2D res)
    {
        res.Instance = _instanceMeshPool.CreateInstance(_instanceMeshPool.AsInstanced(res.Mesh));
    }

    private void DeleteInstance(RenderInstanceMesh2D res)
    {
        if (res.Instance == null) return;
        _instanceMeshPool.DeleteInstance(res.Instance);
        res.Instance = null;
    }

    /// <inheritdoc />
    public override RenderInstanceMesh2D UnRegisterComponent(IEntity entity, RenderInstanceMesh2D component)
    {
        if (!entity.TryGetComponent<UiTransform>(out var transform))
            throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

        var res = base.UnRegisterComponent(entity, component);
        DeleteInstance(res);
        Interlocked.Increment(ref _dataVersion);
        return res;
    }

    public override RenderInstanceMesh2D CreateComponent(IEntity entity)
    {
        if (!entity.TryGetComponent<UiTransform>(out var transform))
            transform = new UiTransform(null, UiAnchor.Zero, UiAnchor.Zero);
        if (!entity.TryGetComponent<TextureComponent>(out var texture))
            texture = new TextureComponent();
        return new RenderInstanceMesh2D(MeshPrefab.Rect, transform, texture);
    }
}