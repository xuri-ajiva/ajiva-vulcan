using System.Runtime.CompilerServices;
using ajiva.Components;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Models;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Layers.Layer3d;
using ajiva.Systems.Assets;
using ajiva.Systems.VulcanEngine.Interfaces;
using ajiva.Systems.VulcanEngine.Layer;
using ajiva.Systems.VulcanEngine.Layers;
using ajiva.Systems.VulcanEngine.Layers.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils.Changing;
using GlmSharp;

namespace ajiva.Systems.VulcanEngine.Debug;

[Dependent(typeof(WindowSystem))]
public class DebugLayer : ComponentSystemBase<DebugComponent>, IInit, IUpdate, IAjivaLayerRenderSystem<UniformViewProj3d>
{
    private readonly object mainLock = new object();
    private IMeshPool meshPool;

    /// <inheritdoc />
    /// <inheritdoc />
    public DebugLayer(IAjivaEcs ecs) : base(ecs)
    {
        GraphicsDataChanged = new ChangingObserver<IAjivaLayerRenderSystem>(this);
    }

    public PipelineDescriptorInfos[] PipelineDescriptorInfos { get; set; }

    public IAChangeAwareBackupBufferOfT<DebugUniformModel> Models { get; set; }

    public Shader MainShader { get; set; }

    public List<DebugComponent> SnapShot { get; set; }

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
            renderGuard.BindDescriptor(render.Id * (uint)Unsafe.SizeOf<DebugUniformModel>());
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
        return CreateDebugPipe.Default(renderPassLayer.Parent, renderPassLayer, Ecs.Get<DeviceSystem>(), true, Vertex3D.GetBindingDescription(), Vertex3D.GetAttributeDescriptions(), MainShader, PipelineDescriptorInfos);
    }

    /// <inheritdoc />
    public Reactive<bool> Render { get; } = new Reactive<bool>(true);

    /// <inheritdoc />
    public IAjivaLayer<UniformViewProj3d> AjivaLayer { get; set; }

    /// <inheritdoc />
    public void Init()
    {
        var deviceSystem = Ecs.Get<DeviceSystem>();

        MainShader = Shader.CreateShaderFrom(Ecs.Get<AssetManager>(), "3d/debug", deviceSystem, "main");
        Models = new AChangeAwareBackupBufferOfT<DebugUniformModel>(Const.Default.ModelBufferSize, deviceSystem);
        meshPool = Ecs.Get<IMeshPool>();

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
        }
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromMilliseconds(15));

    /// <inheritdoc />
    public override DebugComponent RegisterComponent(IEntity entity, DebugComponent component)
    {
        if (!entity.TryGetComponent<Transform3d>(out var transform))
            throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

        component.Models = Models;
        transform.ChangingObserver.OnChanged += component.OnTransformChange;
        component.TransformChange(transform.ModelMat);

        var res = base.RegisterComponent(entity, component);
        GraphicsDataChanged.Changed();
        return res;
    }

    /// <inheritdoc />
    public override DebugComponent UnRegisterComponent(IEntity entity, DebugComponent component)
    {
        if (!entity.TryGetComponent<Transform3d>(out var transform))
            throw new ArgumentException("Entity needs and transform in order to be rendered as debug");

        transform.ChangingObserver.OnChanged -= component.OnTransformChange;

        var res = base.UnRegisterComponent(entity, component);
        GraphicsDataChanged.Changed();
        return res;
    }
}
public struct DebugUniformModel : IComp<DebugUniformModel>
{
    public mat4 Model;

    /// <inheritdoc />
    public bool CompareTo(DebugUniformModel other)
    {
        return other.Model == Model;
    }
}
public class DebugComponent : RenderMeshIdUnique<DebugComponent>
{
    public DebugComponent()
    {
        OnTransformChange = TransformChange;
    }

    public IChangingObserverOnlyValue<mat4>.OnChangedDelegate OnTransformChange { get; }

    public IAChangeAwareBackupBufferOfT<DebugUniformModel>? Models { get; set; }

    public bool DrawTransform { get; set; }
    public bool DrawWireframe { get; set; }
    public bool NoDepthTest { get; set; }

    public void TransformChange(mat4 value)
    {
        if (Models is null)
        {
            ALog.Warn("RenderMeshUpdate Failed!");
            return;
        }

        var change = Models.GetForChange((int)Id);

        change.Value.Model = value;
    }
}
