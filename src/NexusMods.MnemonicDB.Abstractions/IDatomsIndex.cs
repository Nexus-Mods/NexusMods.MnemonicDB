using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.MnemonicDB.Abstractions;

public interface IDatomsIndex
{
    /// <summary>
    /// Get the datoms for a specific descriptor
    /// </summary>
    public IndexSegment Datoms<TDescriptor>(TDescriptor descriptor) where TDescriptor : ISliceDescriptor;

    /// <summary>
    /// Return a chunked sequence of datoms for a specific descriptor, chunks will be of the specified size
    /// except for the last chunk which may be smaller
    /// </summary>
    public IEnumerable<IndexSegment> DatomsChunked<TSliceDescriptor>(TSliceDescriptor descriptor, int chunkSize)
        where TSliceDescriptor : ISliceDescriptor;
}
