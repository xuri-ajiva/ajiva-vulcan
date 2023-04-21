using Ajiva.Systems.VulcanEngine.Systems;

namespace Ajiva.Systems.VulcanEngine.Interfaces;

public interface IWindowSystem
{
    Canvas Canvas { get; }

    event KeyEventHandler? OnKeyEvent;
    event WindowResizedDelegate OnResize;
    event EventHandler<AjivaMouseMotionCallbackEventArgs>? OnMouseMove;
    void EnsureSurfaceExists();
    void InitWindow();
    void CloseWindow();
    void PollEvents();
}
