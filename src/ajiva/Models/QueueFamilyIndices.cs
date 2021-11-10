using System.Runtime.CompilerServices;

namespace ajiva.Models;

public struct QueueFamilyIndices
{
    public uint? GraphicsFamily;
    public uint? PresentFamily;
    public uint? TransferFamily;

    public IEnumerable<uint> Indices
    {
        get
        {
            if (GraphicsFamily.HasValue) yield return GraphicsFamily.Value;

            if (PresentFamily.HasValue && PresentFamily != GraphicsFamily) yield return PresentFamily.Value;

            if (TransferFamily.HasValue && TransferFamily != PresentFamily && TransferFamily != GraphicsFamily) yield return TransferFamily.Value;
        }
    }

    public bool IsComplete
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] get =>
            GraphicsFamily.HasValue
            && PresentFamily.HasValue
            && TransferFamily.HasValue;
    }
}