using System.Runtime.CompilerServices;
using ajiva.Components;
using ajiva.Components.Media;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Models;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Layers.Layer2d;
using ajiva.Systems.Assets;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layer3d;
using ajiva.Systems.VulcanEngine.Layers;
using ajiva.Systems.VulcanEngine.Layers.Creation;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils.Changing;

namespace ajiva.Systems.VulcanEngine.Layer2d;

[Dependent(typeof(Ajiva3dLayerSystem))]
public class Mesh2dRenderLayer : ComponentSystemBase<RenderMesh2D>, IInit, IUpdate, IAjivaLayerRenderSystem<UniformLayer2d>
{
    private readonly object mainLock = new object();
    private MeshPool meshPool;

    /// <inheritdoc />
    public Mesh2dRenderLayer(IAjivaEcs ecs) : base(ecs)
    {
        GraphicsDataChanged = new ChangingObserver<IAjivaLayerRenderSystem>(this);
    }

    public IAChangeAwareBackupBufferOfT<SolidUniformModel2d> Models { get; set; }

    public PipelineDescriptorInfos[] PipelineDescriptorInfos { get; set; }

    public Shader MainShader { get; set; }

    public List<RenderMesh2D> SnapShot { get; set; }
    public IAjivaLayer<UniformLayer2d> AjivaLayer { get; set; }

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
            renderGuard.BindDescriptor(render.Id * (uint)Unsafe.SizeOf<SolidUniformModel2d>());
            readyMeshPool.DrawMesh(renderGuard.Buffer, render.MeshId);
        }
    }

    /// <inheritdoc />
    public object SnapShotLock { get; } = new object();

    /// <inheritdoc />
    public void CreateSnapShot()
    {
        lock (ComponentEntityMap)
        {
            SnapShot = ComponentEntityMap.Keys.Where(x => x.Render).ToList();
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
        var res = GraphicsPipelineLayerCreator.Default(renderPassLayer.Parent, renderPassLayer, Ecs.GetSystem<DeviceSystem>(), true, Vertex2D.GetBindingDescription(), Vertex2D.GetAttributeDescriptions(), MainShader, PipelineDescriptorInfos);

        foreach (var entity in ComponentEntityMap) entity.Key.ChangingObserver.Changed();

        return res;
    }

    /// <inheritdoc />
    public Reactive<bool> Render { get; } = new Reactive<bool>(false);

    /// <inheritdoc />
    public void Init()
    {
        var deviceSystem = Ecs.GetSystem<DeviceSystem>();

        MainShader = Shader.CreateShaderFrom(Ecs.GetSystem<AssetManager>(), "2d", deviceSystem, "main");
        Models = new AChangeAwareBackupBufferOfT<SolidUniformModel2d>(1000000, deviceSystem);
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
        {
            Models.CommitChanges();
        }
    }

    /// <inheritdoc />
    public override RenderMesh2D RegisterComponent(IEntity entity, RenderMesh2D component)
    {
        if (!entity.TryGetComponent<Transform2d>(out var transform))
            throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

        component.Models = Models;
        transform.ChangingObserver.OnChanged += component.OnTransformChange;

        var res = base.RegisterComponent(entity, component);
        GraphicsDataChanged.Changed();
        return res;
    }

    /// <inheritdoc />
    public override RenderMesh2D UnRegisterComponent(IEntity entity, RenderMesh2D component)
    {
        if (!entity.TryGetComponent<Transform2d>(out var transform))
            throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

        transform.ChangingObserver.OnChanged -= component.OnTransformChange;

        return base.UnRegisterComponent(entity, component);
    }
}