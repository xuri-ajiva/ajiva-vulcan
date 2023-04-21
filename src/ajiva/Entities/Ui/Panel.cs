using Ajiva.Components.Transform.Ui;
using Ajiva.Ecs.Entity.Helper;

namespace Ajiva.Entities.Ui;

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
