using ajiva.Components.Transform.Ui;
using ajiva.Ecs.Entity.Helper;

namespace ajiva.Entities.Ui;

[EntityComponent(typeof(UiTransform))]

public partial class Panel 
{
    public Panel(UiTransform uiTransform)
    {
        UiTransform = uiTransform;
    }

    public void AddChild(IUiTransform child)
    {
        UiTransform.AddChild(child);
    }
}
