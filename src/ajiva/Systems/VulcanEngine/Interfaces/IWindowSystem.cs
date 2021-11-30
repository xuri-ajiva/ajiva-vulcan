using ajiva.Ecs;
using ajiva.Models;
using ajiva.Systems.VulcanEngine.Systems;

namespace ajiva.Systems.VulcanEngine.Interfaces;

public interface IWindowSystem : IAjivaEcsObject
{
    Canvas Canvas { get; }

    event KeyEventHandler? OnKeyEvent;
    event Action? OnResize;
    event EventHandler<AjivaMouseMotionCallbackEventArgs>? OnMouseMove;
    void EnsureSurfaceExists();
    void InitWindow();
    void CloseWindow();
    void PollEvents();
}
