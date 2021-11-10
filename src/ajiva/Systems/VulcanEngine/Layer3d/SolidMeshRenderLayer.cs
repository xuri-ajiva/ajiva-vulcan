using System.Runtime.CompilerServices;
using ajiva.Components;
using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Models;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Layers.Layer3d;
using ajiva.Systems.Assets;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers;
using ajiva.Systems.VulcanEngine.Layers.Creation;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils.Changing;

namespace ajiva.Systems.VulcanEngine.Layer3d;

[Dependent(typeof(Ajiva3dLayerSystem))]
public class SolidMeshRenderLayer : ComponentSystemBase<RenderMesh3D>, IInit, IUpdate, IAjivaLayerRenderSystem<UniformViewProj3d>
{
    private MeshPool meshPool;
    private readonly object mainLock = new();

    public IAChangeAwareBackupBufferOfT<SolidUniformModel> Models { get; set; }

    /// <inheritdoc />
    public override RenderMesh3D RegisterComponent(IEntity entity, RenderMesh3D component)
    {
        if (!entity.TryGetComponent<Transform3d>(out var transform))
            throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

        component.Models = Models;
        transform.ChangingObserver.OnChanged += component.OnTransformChange;

        if (entity.TryGetComponent<TextureComponent>(out var texture))
        {
            component.TextureComponent = texture;
        }

        //component.ChangingObserver.OnChanged += _ => Models.GetForChange((int)component.Id).Value.TextureSamplerId = component.Id;

        var res = base.RegisterComponent(entity, component);
        GraphicsDataChanged.Changed();
        return res;
    }

    /// <inheritdoc />
    public override RenderMesh3D UnRegisterComponent(IEntity entity, RenderMesh3D component)
    {
        if (!entity.TryGetComponent<Transform3d>(out var transform))
            throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

        transform.ChangingObserver.OnChanged -= component.OnTransformChange;

        var res = base.UnRegisterComponent(entity, component);
        GraphicsDataChanged.Changed();
        return res;
    }

    /// <inheritdoc />
    public SolidMeshRenderLayer(IAjivaEcs ecs) : base(ecs)
    {
        GraphicsDataChanged = new ChangingObserver<IAjivaLayerRenderSystem>(this);
    }

    public PipelineDescriptorInfos[] PipelineDescriptorInfos { get; set; }

    public Shader MainShader { get; set; }
    public IAjivaLayer<UniformViewProj3d> AjivaLayer { get; set; }

    /// <inheritdoc />
    public IChangingObserver<IAjivaLayerRenderSystem> GraphicsDataChanged { get; }

    /// <inheritdoc />
    public void DrawComponents(RenderLayerGuard renderGuard, CancellationToken cancellationToken)
    {
        var readyMeshPool = meshPool.Use();

        foreach (var render in SnapShot)
        {
            if (cancellationToken.IsCancellationRequested) return;
            if (!render.Render) continue;
            renderGuard.BindDescriptor(render.Id * (uint)Unsafe.SizeOf<SolidUniformModel>());
            readyMeshPool.DrawMesh(renderGuard.Buffer, render.MeshId);
        }
    }

    public List<RenderMesh3D> SnapShot { get; set; }

    /// <inheritdoc />
    public object SnapShotLock { get; } = new();

    /// <inheritdoc />
    public void CreateSnapShot()
    {
        lock (ComponentEntityMap)
            SnapShot = ComponentEntityMap.Keys.Where(x => x.Render).ToList();
    }

    /// <inheritdoc />
    public void ClearSnapShot()
    {
        SnapShot = null!;
    }

    /// <inheritdoc />
    public GraphicsPipelineLayer CreateGraphicsPipelineLayer(RenderPassLayer renderPassLayer)
    {
        var res = GraphicsPipelineLayerCreator.Default(renderPassLayer.Parent, renderPassLayer, Ecs.GetSystem<DeviceSystem>(), true, Vertex3D.GetBindingDescription(), Vertex3D.GetAttributeDescriptions(), MainShader, PipelineDescriptorInfos);

        foreach (var entity in ComponentEntityMap)
        {
            entity.Key.ChangingObserver.Changed();
        }

        return res;
    }

    /// <inheritdoc />
    public Reactive<bool> Render { get; } = new Reactive<bool>(true);

    /// <inheritdoc />
    public void Init()
    {
        var deviceSystem = Ecs.GetSystem<DeviceSystem>();

        MainShader = Shader.CreateShaderFrom(Ecs.GetSystem<AssetManager>(), "3d/Solid", deviceSystem, "main");
        Models = new AChangeAwareBackupBufferOfT<SolidUniformModel>(Const.Default.ModelBufferSize, deviceSystem);
        meshPool = Ecs.GetInstance<MeshPool>();

        PipelineDescriptorInfos = Layers.PipelineDescriptorInfos.CreateFrom(
            AjivaLayer.LayerUniform.Uniform.Buffer!, (uint)AjivaLayer.LayerUniform.SizeOfT,
            Models.Uniform.Buffer!, (uint)Models.SizeOfT,
            Ecs.GetComponentSystem<TextureSystem, TextureComponent>().TextureSamplerImageViews
        );
    }

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        lock (mainLock)
            Models.CommitChanges();
    }
}