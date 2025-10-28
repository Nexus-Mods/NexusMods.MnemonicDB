using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;

namespace NexusMods.MnemonicDB.InternalTxFunctions;

/// <summary>
/// A standard transaction that simply processes an index segment
/// </summary>
internal sealed class IndexSegmentTransaction : AInternalFn
{
    private readonly Datoms _indexSegment;

    /// <summary>
    /// A standard transaction that simply processes an index segment
    /// </summary>
    public IndexSegmentTransaction(Datoms indexSegment)
    {
        _indexSegment = indexSegment;
    }

    /// <inheritdoc />
    public override void Execute(DatomStore store, AttributeResolver resolver)
    {
        store.LogDatoms(_indexSegment);
    }
}
