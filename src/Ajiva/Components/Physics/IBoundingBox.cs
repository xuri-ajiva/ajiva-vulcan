﻿using Ajiva.Components.Transform.SpatialAcceleration;
using Ajiva.Systems.VulcanEngine.Debug;

namespace Ajiva.Components.Physics;

public interface IBoundingBox : IComponent
{
    StaticOctalSpace Space { get; }
    void ComputeBoxBackground();
    void SetData(IStaticOctalTreeContainer<BoundingBox> octalTree, IDebugVisualPool debugVisualPool);
    void RemoveData();
}
