using System.Runtime.InteropServices;
using ajiva.Components;
using ajiva.Components.Media;
using ajiva.Components.Mesh;
using ajiva.Components.Mesh.Instance;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Models;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Instance;
using ajiva.Models.Layers.Layer3d;
using ajiva.Systems.Assets;
using ajiva.Systems.VulcanEngine.Interfaces;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers;
using ajiva.Systems.VulcanEngine.Layers.Creation;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils.Changing;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Layer3d;

[Dependent(typeof(Ajiva3dLayerSystem))]
public class SolidMeshRenderLayer : ComponentSystemBase<RenderInstanceMesh>, IInit, IUpdate, IAjivaLayerRenderSystem<UniformViewProj3d>
{
    private readonly object mainLock = new object();
    private IMeshPool meshPool;
    private IInstanceMeshPool instanceMeshPool;

    /// <inheritdoc />
    public SolidMeshRenderLayer(IAjivaEcs ecs) : base(ecs)
    {
        GraphicsDataChanged = new ChangingObserver<IAjivaLayerRenderSystem>(this);
    }

    public IAChangeAwareBackupBufferOfT<SolidUniformModel> Models { get; set; }
    public IAChangeAwareBackupBufferOfT<MeshInstanceData> InstanceData { get; set; }

    public PipelineDescriptorInfos[] PipelineDescriptorInfos { get; set; }

    public Shader MainShader { get; set; }

    public IEnumerable<IInstancedMesh> SnapShot { get; set; }
    public IAjivaLayer<UniformViewProj3d> AjivaLayer { get; set; }

    /// <inheritdoc />
    public IChangingObserver<IAjivaLayerRenderSystem> GraphicsDataChanged { get; }

    /// <inheritdoc />
    public void DrawComponents(RenderLayerGuard renderGuard, CancellationToken cancellationToken)
    {
        renderGuard.BindDescriptor(0);

        foreach (var instancedMesh in SnapShot)
        {
            instanceMeshPool.DrawInstanced(instancedMesh, renderGuard.Buffer, VERTEX_BUFFER_BIND_ID, INSTANCE_BUFFER_BIND_ID);
        }
        /*//renderGuard.Buffer.BindDescriptorSets(PipelineBindPoint.Compute, renderGuard.Pipeline.PipelineLayout, 0, renderGuard.Pipeline.DescriptorSet, 0);
        var readyMeshPool = meshPool.Use();
        foreach (var render in SnapShot.Where(x => x.Render).GroupBy(x => x.MeshId))
        {
            var mesh3Ds = render.ToList();

            if (cancellationToken.IsCancellationRequested) return;
            renderGuard.BindDescriptor(0);
            var mesh = (Mesh<Vertex3D>)meshPool.GetMesh(render.Key);

            renderGuard.Buffer.BindVertexBuffers(VERTEX_BUFFER_BIND_ID, mesh.VertexBuffer.Buffer, 0);
            renderGuard.Buffer.BindVertexBuffers(INSTANCE_BUFFER_BIND_ID, InstanceData.Uniform.Buffer, 0);
            renderGuard.Buffer.BindIndexBuffer(mesh.IndexBuffer.Buffer, 0, IndexType.Uint16);
            renderGuard.Buffer.DrawIndexed((uint)mesh.IndexBuffer.Length, (uint)mesh3Ds.Count, 0, 0, 0);
        }*/
    }

    /// <inheritdoc />
    public object SnapShotLock { get; } = new object();

    /// <inheritdoc />
    public void CreateSnapShot()
    {
        lock (ComponentEntityMap)
        {
            SnapShot = ComponentEntityMap.Keys.Select(x => x.Instance.InstancedMesh).DistinctBy(x => x.InstancedId).ToList();
        }
    }

    /// <inheritdoc />
    public void ClearSnapShot()
    {
        SnapShot = null!;
    }

    /// <inheritdoc />
    public GraphicsPipelineLayer CreateGraphicsPipelineLayer(RenderPassLayer renderPassLayer)
    {
        var bind = new[]
        {
            new VertexInputBindingDescription(VERTEX_BUFFER_BIND_ID, (uint)Marshal.SizeOf<Vertex3D>(), VertexInputRate.Vertex),
            new VertexInputBindingDescription(INSTANCE_BUFFER_BIND_ID, (uint)Marshal.SizeOf<MeshInstanceData>(), VertexInputRate.Instance)
        };
        var attrib = new[]
        {
            new VertexInputAttributeDescription(0, VERTEX_BUFFER_BIND_ID, Format.R32G32B32SFloat, (uint)Marshal.OffsetOf<Vertex3D>(nameof(Vertex3D.Position))),
            new VertexInputAttributeDescription(1, VERTEX_BUFFER_BIND_ID, Format.R32G32B32SFloat, (uint)Marshal.OffsetOf<Vertex3D>(nameof(Vertex3D.Colour))),
            new VertexInputAttributeDescription(2, VERTEX_BUFFER_BIND_ID, Format.R32G32SFloat, (uint)Marshal.OffsetOf<Vertex3D>(nameof(Vertex3D.TextCoord))),
            new VertexInputAttributeDescription(3, INSTANCE_BUFFER_BIND_ID, Format.R32G32B32SFloat, (uint)Marshal.OffsetOf<MeshInstanceData>(nameof(MeshInstanceData.Position))),
            new VertexInputAttributeDescription(4, INSTANCE_BUFFER_BIND_ID, Format.R32G32B32SFloat, (uint)Marshal.OffsetOf<MeshInstanceData>(nameof(MeshInstanceData.Rotation))),
            new VertexInputAttributeDescription(5, INSTANCE_BUFFER_BIND_ID, Format.R32G32B32SFloat, (uint)Marshal.OffsetOf<MeshInstanceData>(nameof(MeshInstanceData.Scale))),
            new VertexInputAttributeDescription(6, INSTANCE_BUFFER_BIND_ID, Format.R32SInt, (uint)Marshal.OffsetOf<MeshInstanceData>(nameof(MeshInstanceData.TextureIndex))),
            new VertexInputAttributeDescription(7, INSTANCE_BUFFER_BIND_ID, Format.R32G32SFloat, (uint)Marshal.OffsetOf<MeshInstanceData>(nameof(MeshInstanceData.Padding))),
        };

        /*
         *
            new VertexInputAttributeDescription(3, INSTANCE_BUFFER_BIND_ID, Format.R32G32B32A32SFloat, (uint)Marshal.OffsetOf<MeshInstanceData>(nameof(MeshInstanceData.Model1))),
            new VertexInputAttributeDescription(4, INSTANCE_BUFFER_BIND_ID, Format.R32G32B32A32SFloat, (uint)Marshal.OffsetOf<MeshInstanceData>(nameof(MeshInstanceData.Model2))),
            new VertexInputAttributeDescription(5, INSTANCE_BUFFER_BIND_ID, Format.R32G32B32A32SFloat, (uint)Marshal.OffsetOf<MeshInstanceData>(nameof(MeshInstanceData.Model3))),
            new VertexInputAttributeDescription(6, INSTANCE_BUFFER_BIND_ID, Format.R32G32B32A32SFloat, (uint)Marshal.OffsetOf<MeshInstanceData>(nameof(MeshInstanceData.TextureSamplerId))),
         * 
         */
        var res = GraphicsPipelineLayerCreator.Default(renderPassLayer.Parent, renderPassLayer, Ecs.Get<DeviceSystem>(), true,
            bind,
            attrib,
            MainShader, PipelineDescriptorInfos);

        foreach (var entity in ComponentEntityMap)
        {
            //todo?
            //entity.Key.ChangingObserver.Changed();
        }

        return res;
    }

    private const uint VERTEX_BUFFER_BIND_ID = 0;
    private const uint INSTANCE_BUFFER_BIND_ID = 1;

    /// <inheritdoc />
    public Reactive<bool> Render { get; } = new Reactive<bool>(true);

    /// <inheritdoc />
    public void Init()
    {
        var deviceSystem = Ecs.Get<DeviceSystem>();

        MainShader = Shader.CreateShaderFrom(Ecs.Get<AssetManager>(), "3d/SolidInstance", deviceSystem, "main");
        Models = new AChangeAwareBackupBufferOfT<SolidUniformModel>(Const.Default.ModelBufferSize, deviceSystem);
        InstanceData = new AChangeAwareBackupBufferOfT<MeshInstanceData>(Const.Default.ModelBufferSize, deviceSystem, BufferUsageFlags.VertexBuffer);
        meshPool = Ecs.Get<IMeshPool>();
        instanceMeshPool = Ecs.Get<IInstanceMeshPool>();

        PipelineDescriptorInfos = Layers.PipelineDescriptorInfos.CreateFrom(
            AjivaLayer.LayerUniform.Uniform.Buffer!, (uint)AjivaLayer.LayerUniform.SizeOfT,
            Models.Uniform.Buffer!, (uint)Models.SizeOfT,
            Ecs.Get<ITextureSystem>().TextureSamplerImageViews
        );
    }

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        lock (mainLock)
        {
            Models.CommitChanges();
            InstanceData.CommitChanges();
        }
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromMilliseconds(15));

    /// <inheritdoc />
    public override RenderInstanceMesh RegisterComponent(IEntity entity, RenderInstanceMesh component)
    {
        if (!entity.TryGetComponent<Transform3d>(out var transform))
            throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

        transform.ChangingObserver.OnChanged += component.OnTransformChange;
        component.TransformChange(transform.ModelMat);

        if (entity.TryGetComponent<TextureComponent>(out var texture)) component.TextureComponent = texture;

        //component.ChangingObserver.OnChanged += _ => Models.GetForChange((int)component.Id).Value.TextureSamplerId = component.Id;

        var res = base.RegisterComponent(entity, component);
        GraphicsDataChanged.Changed();
        return res;
    }

    /// <inheritdoc />
    public override RenderInstanceMesh UnRegisterComponent(IEntity entity, RenderInstanceMesh component)
    {
        if (!entity.TryGetComponent<Transform3d>(out var transform))
            throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

        transform.ChangingObserver.OnChanged -= component.OnTransformChange;

        var res = base.UnRegisterComponent(entity, component);
        GraphicsDataChanged.Changed();
        return res;
    }
}
