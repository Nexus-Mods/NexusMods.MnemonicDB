using System;
using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.Implementation.Diffs;

namespace NexusMods.MnemonicDB.Abstractions.Cascade;

public static class FlowExtensions
{
    /// <summary>
    /// Returns a window of the last n items in the flow, in order. After the window is full, the oldest item is removed.
    /// </summary>
    public static IDiffFlow<T[]> SlidingWindow<T>(this IFlow<T> upstream, int windowSize)
    {
        return Flow.SlidingWindow(upstream, windowSize);
        
    }
    
}
