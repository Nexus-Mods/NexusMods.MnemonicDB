using System.Collections.Generic;

namespace NexusMods.DatomStore;

public interface IBlock
{
    /// <summary>
    /// The version/format of the block
    /// </summary>
    public byte Version { get; }

    /// <summary>
    /// The the number of datoms in the block
    /// </summary>
    public ushort DatomCount { get; }

}
