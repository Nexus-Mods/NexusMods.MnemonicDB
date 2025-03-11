using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

/// <summary>
/// An enumerator that filters out all datoms that are newer than a given transaction id
/// </summary>
public struct TimeFilteredEnumerator<TInner> : IRefDatomEnumerator 
    where TInner : IRefDatomEnumerator
{
    private TInner _inner;
    private readonly TxId _txId;

    public TimeFilteredEnumerator(TInner inner, TxId txId)
    {
        _inner = inner;
        _txId = txId;
    }
    
    public void Dispose()
    {
        _inner.Dispose();
    }

    public bool MoveNext<TSliceDescriptor>(TSliceDescriptor descriptor, bool useHistory = false) 
        where TSliceDescriptor : ISliceDescriptor, allows ref struct
    {
        while (_inner.MoveNext(descriptor, useHistory))
        {
            if (_inner.KeyPrefix.T <= _txId)
                return true;
        }

        return false;
    }

    public KeyPrefix KeyPrefix => _inner.KeyPrefix;
    public Ptr Current => _inner.Current;
    public Ptr ValueSpan => _inner.ValueSpan;
    public Ptr ExtraValueSpan => _inner.ExtraValueSpan;
}
