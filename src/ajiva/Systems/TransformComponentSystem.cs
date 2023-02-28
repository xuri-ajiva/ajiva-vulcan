using ajiva.Components.Transform;
using ajiva.Components.Transform.Ui;
using ajiva.Ecs;
using ajiva.Systems.VulcanEngine.Interfaces;

namespace ajiva.Systems;

public class TransformComponentSystem : ComponentSystemBase<Transform3d>, ITransformComponentSystem
{
    private Random r = new Random();

    public TransformComponentSystem(IAjivaEcs ecs) : base(ecs)
    {
    }
}
public class Transform2dComponentSystem : ComponentSystemBase<UiTransform>, ITransform2dComponentSystem, IInit
{
    private readonly IWindowSystem windowSystem;
    public UIRootTransform RootTransform { get; set; }

    public Transform2dComponentSystem(IAjivaEcs ecs,IWindowSystem windowSystem) : base(ecs)
    {
        this.windowSystem = windowSystem;
        Init();
    }

    /// <inheritdoc />
    public void Init()
    {
        RootTransform = new UIRootTransform(windowSystem.Canvas.WidthI, windowSystem.Canvas.HeightI, -1.0f, 1.0f);
        windowSystem.OnResize += (sender, oldExtent, newSize) =>
        {
            RootTransform.DisplaySize = new Rect2Di(0, 0, (int)newSize.Width, (int)newSize.Height);
        };
    }

    /// <inheritdoc />
    public override UiTransform RegisterComponent(IEntity entity, UiTransform component)
    {
        if (component.Parent is null)
        {
            RootTransform.AddChild(component);
        }
        return base.RegisterComponent(entity, component);
    }

    /// <inheritdoc />
    public override UiTransform UnRegisterComponent(IEntity entity, UiTransform component)
    {
        if (component.Parent == RootTransform)
        {
            RootTransform.RemoveChild(component);
        }
        return base.UnRegisterComponent(entity, component);
    }
}
