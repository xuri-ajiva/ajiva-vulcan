﻿using System.Runtime.InteropServices;
using ajiva.Components;
using ajiva.Components.Media;
using ajiva.Components.Mesh;
using ajiva.Components.Mesh.Instance;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Models.Instance;
using ajiva.Models.Layers.Layer3d;
using ajiva.Models.Vertex;
using ajiva.Systems.Assets;
using ajiva.Systems.VulcanEngine.Interfaces;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers;
using ajiva.Systems.VulcanEngine.Layers.Creation;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Layer3d;

public class SolidMeshRenderLayer : ComponentSystemBase<RenderInstanceMesh>, IAjivaLayerRenderSystem<UniformViewProj3d>, IUpdate
{
    //todo remove some duplicates between Layers
    private readonly IDeviceSystem _deviceSystem;
    private readonly ITextureSystem _textureSystem;
    private InstanceMeshPool<MeshInstanceData> instanceMeshPool;
    private long dataVersion;

    /// <inheritdoc />
    public SolidMeshRenderLayer(IDeviceSystem deviceSystem, AssetManager assetManager, ITextureSystem textureSystem)
    {
        _deviceSystem = deviceSystem;
        _textureSystem = textureSystem;
        MainShader = Shader.CreateShaderFrom(assetManager, "3d/SolidInstance", deviceSystem, "main");
        instanceMeshPool = new InstanceMeshPool<MeshInstanceData>(deviceSystem);
        instanceMeshPool.Changed.OnChanged += RebuildData;
    }

    public PipelineDescriptorInfos[]? PipelineDescriptorInfos { get; set; }

    public Shader MainShader { get; set; }
    public IAjivaLayer<UniformViewProj3d> AjivaLayer { get; set; }

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

        if (PipelineDescriptorInfos == null)
            CreatePipelineDescriptorInfos();

        RenderTarget.GraphicsPipelineLayer = GraphicsPipelineLayerCreator.Default(RenderTarget.PassLayer.Parent, RenderTarget.PassLayer,
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

    private void RebuildData(IInstanceMeshPool<MeshInstanceData> sender)
    {
        Interlocked.Increment(ref dataVersion);
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        base.ReleaseUnmanagedResources(disposing);
        instanceMeshPool.Dispose();
    }

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        instanceMeshPool.Update(delta);
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromMilliseconds(10));

    /// <inheritdoc />
    public override RenderInstanceMesh RegisterComponent(IEntity entity, RenderInstanceMesh component)
    {
        if (!entity.TryGetComponent<Transform3d>(out var transform))
            throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

        var res = base.RegisterComponent(entity, component);
        CreateInstance(res);
        Interlocked.Increment(ref dataVersion);
        return res;
    }

    private void CreateInstance(RenderInstanceMesh res)
    {
        res.Instance = instanceMeshPool.CreateInstance(instanceMeshPool.AsInstanced(res.Mesh));
    }

    private void DeleteInstance(RenderInstanceMesh res)
    {
        if (res.Instance == null) return;
        instanceMeshPool.DeleteInstance(res.Instance);
        res.Instance = null;
    }

    /// <inheritdoc />
    public override RenderInstanceMesh UnRegisterComponent(IEntity entity, RenderInstanceMesh component)
    {
        if (!entity.TryGetComponent<Transform3d>(out var transform))
            throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

        var res = base.UnRegisterComponent(entity, component);
        DeleteInstance(res);
        Interlocked.Increment(ref dataVersion);
        return res;
    }

    public override RenderInstanceMesh CreateComponent(IEntity entity)
    {
        if (!entity.TryGetComponent<Transform3d>(out var transform))
            transform = new Transform3d();
        if(!entity.TryGetComponent<TextureComponent>(out var texture))
            texture = new TextureComponent();
        return new RenderInstanceMesh(MeshPrefab.Rect, transform, texture);
    }
}
