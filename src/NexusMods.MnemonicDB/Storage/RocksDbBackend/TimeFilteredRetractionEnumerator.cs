using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

public struct TimeFilteredRetractionEnumerator<TInner> : IRefDatomEnumerator 
    where TInner : IRefDatomPeekingEnumerator
{
    private TInner _inner;
    private readonly TxId _txId;

    public TimeFilteredRetractionEnumerator(TInner inner, TxId txId)
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
            if (_inner.TryGetRetractionId(out var id) && id <= _txId)
            {
                _inner.MoveNext(descriptor, useHistory);
                continue;
            }

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
