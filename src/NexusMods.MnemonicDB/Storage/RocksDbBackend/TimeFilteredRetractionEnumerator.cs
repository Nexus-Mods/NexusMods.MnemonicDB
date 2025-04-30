using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend
{
    /// <summary>
    /// An enumerator that filters out all datoms that are newer than the given transaction id, and processes transactions
    /// so that the database is "rewound" to the state it was at the given transaction id.
    /// </summary>
    /// <typeparam name="TInner"></typeparam>
    public struct TimeFilteredRetractionEnumerator<TInner> : IRefDatomEnumerator 
        where TInner : IRefDatomPeekingEnumerator
    {
        private TInner _inner;
        private readonly TxId _txId;
        private Ptr _key;
        private Ptr _value;
        private Ptr _extraValue;
        
        // Cached next values
        private Ptr _nextKey;
        private Ptr _nextValue;
        private Ptr _nextExtraValue;
        private bool _isNextSet;
        private bool _atEnd;

        public TimeFilteredRetractionEnumerator(TInner inner, TxId txId)
        {
            _inner = inner;
            _txId = txId;
            _isNextSet = false;
            _atEnd = false;
            _key = default;
            _value = default;
            _extraValue = default;
            _nextKey = default;
            _nextValue = default;
            _nextExtraValue = default;
        }
        
        public void Dispose() => _inner.Dispose();

        public bool MoveNext<TSliceDescriptor>(TSliceDescriptor descriptor, bool useHistory = false) 
            where TSliceDescriptor : ISliceDescriptor, allows ref struct
        {
            // We continue iterating until we find a valid datom or run out
            while (true)
            {
                // If there is a cached next value, use that as current.
                if (_isNextSet)
                {
                    _key = _nextKey;
                    _value = _nextValue;
                    _extraValue = _nextExtraValue;
                    _isNextSet = false;
                }
                else
                {
                    if (_atEnd)
                    {
                        // If we are at the end of the inner enumerator, return false.
                        return false;
                    }
                    
                    // Otherwise, try to load the next datom from the inner enumerator.
                    _atEnd = !_inner.MoveNext(descriptor, useHistory);
                    if (_atEnd)
                        return false;
                    
                    // Cache the current value from the inner enumerator
                    _key = _inner.Current;
                    _value = _inner.ValueSpan;
                    _extraValue = _inner.ExtraValueSpan;
                }

                // Now, attempt to peek one item ahead
                _atEnd = !_inner.MoveNext(descriptor, useHistory);
                if (!_atEnd)
                {
                    _nextKey = _inner.Current;
                    _nextValue = _inner.ValueSpan;
                    _nextExtraValue = _inner.ExtraValueSpan;
                    _isNextSet = true;
                }
                else
                {
                    _isNextSet = false;
                }

                // If there is a peeked datom, use its key prefix to determine whether it is a retraction.
                if (_isNextSet)
                {
                    var peekPrefix = _nextKey.Read<KeyPrefix>(0);
                    if (peekPrefix.IsRetract && peekPrefix.T <= _txId)
                    {
                        // A retraction exists for the current datom.
                        // Consume the peeked retraction by discarding the cache and skipping to the next iteration.
                        _isNextSet = false;
                        continue;
                    }
                }

                // Verify that the current datom is not dated after _txId.
                var currentPrefix = _key.Read<KeyPrefix>(0);
                if (currentPrefix.T <= _txId)
                    return true;
                // Otherwise, skip this datom and continue iterating.
            }
        }

        public KeyPrefix KeyPrefix => _key.Read<KeyPrefix>(0);
        public Ptr Current => _key;
        public Ptr ValueSpan => _value;
        public Ptr ExtraValueSpan => _extraValue;
    }
}
