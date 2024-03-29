﻿using Ajiva.Components.Transform;
using Ajiva.Components.Transform.Ui;
using Ajiva.Systems.VulcanEngine.Interfaces;

namespace Ajiva.Systems;

public class TransformComponentSystem : ComponentSystemBase<Transform3d>, ITransformComponentSystem
{
    public override Transform3d RegisterComponent(IEntity entity, Transform3d component)
    {
        var ers = base.RegisterComponent(entity, component);
        ers.UpdateAll();
        return ers;
    }

    public override Transform3d CreateComponent(IEntity entity)
    {
        return new Transform3d();
    }
}
public class Transform2dComponentSystem : ComponentSystemBase<UiTransform>, ITransform2dComponentSystem
{
    private readonly IWindowSystem windowSystem;

    public Transform2dComponentSystem(IWindowSystem windowSystem)
    {
        this.windowSystem = windowSystem;
        RootTransform = new UIRootTransform(this.windowSystem.Canvas.WidthI, this.windowSystem.Canvas.HeightI, -1.0f, 1.0f);
        this.windowSystem.OnResize += (sender, oldExtent, newSize) => { RootTransform.DisplaySize = new Rect2Di(0, 0, (int)newSize.Width, (int)newSize.Height); };
    }

    public UIRootTransform RootTransform { get; set; }

    /// <inheritdoc />
    public override UiTransform RegisterComponent(IEntity entity, UiTransform component)
    {
        if (component.Parent is null) RootTransform.AddChild(component);
        return base.RegisterComponent(entity, component);
    }

    /// <inheritdoc />
    public override UiTransform UnRegisterComponent(IEntity entity, UiTransform component)
    {
        if (component.Parent == RootTransform) RootTransform.RemoveChild(component);
        return base.UnRegisterComponent(entity, component);
    }

    public override UiTransform CreateComponent(IEntity entity)
    {
        return new UiTransform(RootTransform, UiAnchor.Zero, UiAnchor.Zero);
    }
}