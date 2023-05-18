using System.Collections.Concurrent;
using System.Numerics;
using Ajiva.Components.Transform.SpatialAcceleration;
using Ajiva.Entities;
using Ajiva.Extensions;

namespace Ajiva.Systems.VulcanEngine.Debug;

public record DebugVisualPool(EntityFactory Factory) : IDebugVisualPool
{
    private readonly ConcurrentDictionary<object, DebugBox> _visuals = new ConcurrentDictionary<object, DebugBox>();
    private readonly ConcurrentBag<DebugBox> _unusedVisuals = new ConcurrentBag<DebugBox>();

    public void UpdateVisual(object owner, StaticOctalSpace area)
    {
        var visual = GetOrCreateVisual(owner);

        var scale = area.Size / 2.0f;
        var position = area.Position + scale;

        visual.Transform3d.Position = position;
        visual.Transform3d.Scale = scale;

        visual.Transform3d.RefPosition((ref Vector3 vec) =>
        {
            vec.X = position.X;
            vec.Y = position.Y;
            vec.Z = position.Z;
        });

        visual.Transform3d.RefScale((ref Vector3 vec) =>
        {
            vec.X = scale.X;
            vec.Y = scale.Y;
            vec.Z = scale.Z;
        });
    }

    private DebugBox GetOrCreateVisual(object owner)
    {
        if (_visuals.TryGetValue(owner, out var visual))
        {
            return visual;
        }

        _unusedVisuals.TryTake(out var newVisual);
        newVisual ??= Factory
            .CreateDebugBox()
            .Finalize();
        _visuals.TryAdd(owner, newVisual);
        return newVisual;
    }

    public void DestroyVisual(object owner)
    {
        if (!_visuals.TryRemove(owner, out var visual)) return;
        //Log.Information("Destroying visual for {area}", visual);

        visual.Transform3d.Scale = Vector3.Zero;
        _unusedVisuals.Add(visual);
    }

    public void CreateVisual(object owner, StaticOctalSpace area)
    {
        //Log.Information("Created visual for {area}", area);
        UpdateVisual(owner, area);
    }
}
