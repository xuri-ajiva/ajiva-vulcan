﻿namespace Ajiva.Components.RenderAble;

public interface IRenderMesh : IComponent
{
    bool Render { get; set; }
    uint MeshId { get; set; }
    uint Id { get; set; }
}