using ajiva.Components.Transform.Ui;
using ajiva.Ecs;

namespace ajiva.Entities.Ui;

public class Panel : DefaultEntity
{
    private readonly UiTransform uiTransform;

    public Panel(UiTransform uiTransform)
    {
        this.uiTransform = uiTransform;
        this.AddComponent(uiTransform);
    }

    public void AddChild(IUiTransform child)
    {
        uiTransform.AddChild(child);
    }
}
