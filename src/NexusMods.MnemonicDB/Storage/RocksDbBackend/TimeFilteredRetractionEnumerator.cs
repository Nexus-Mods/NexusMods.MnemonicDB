using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend
{
    /// <summary>
    /// An enumerator that returns all datoms whose timestamp is less than or equal to a given transaction id.
    /// In addition, if an addition is immediately followed by its corresponding retraction, the pair is filtered out.
    /// </summary>
    /// <typeparam name="TInner"></typeparam>
    public struct TimeFilteredRetractionEnumerator<TInner> : IRefDatomEnumerator
        where TInner : IRefDatomPeekingEnumerator
    {
        private TInner _inner;
        private readonly TxId _txId;
        private bool _isNextSet;
        private bool _atEnd;

        // Caches for the current item.
        private PtrCache _keyCache;
        private PtrCache _extraCache;
        // Separate caches for the peeked item.
        private PtrCache _nextKeyCache;
        private PtrCache _nextExtraCache;

        public TimeFilteredRetractionEnumerator(TInner inner, TxId txId)
        {
            _inner = inner;
            _txId = txId;
            _isNextSet = false;
            _atEnd = false;
            _keyCache = new PtrCache();
            _extraCache = new PtrCache();
            _nextKeyCache = new PtrCache();
            _nextExtraCache = new PtrCache();
        }

        public void Dispose() => _inner.Dispose();

        public bool MoveNext<TSliceDescriptor>(TSliceDescriptor descriptor, bool useHistory = false)
            where TSliceDescriptor : ISliceDescriptor, allows ref struct
        {
            while (true)
            {
                // If we have a peeked datom from a previous iteration, swap it in.
                if (_isNextSet)
                {
                    _keyCache.Swap(_nextKeyCache);
                    _extraCache.Swap(_nextExtraCache);
                    _isNextSet = false;
                }
                else
                {
                    if (_atEnd)
                        return false;
                    if (!_inner.MoveNext(descriptor, useHistory))
                    {
                        _atEnd = true;
                        return false;
                    }
                    // Copy inner values to our own caches.
                    _keyCache.CopyFrom(_inner.Current);
                    _extraCache.CopyFrom(_inner.ExtraValueSpan);
                }

                // If the current datom's timestamp is greater than txId, skip it.
                var currentPrefix = _keyCache.Ptr.Read<KeyPrefix>(0);
                if (currentPrefix.T > _txId)
                {
                    // Continue to the next datom.
                    continue;
                }

                // Attempt to peek the next datom.
                if (_inner.MoveNext(descriptor, useHistory))
                {
                    _nextKeyCache.CopyFrom(_inner.Current);
                    _nextExtraCache.CopyFrom(_inner.ExtraValueSpan);
                    _isNextSet = true;
                }
                else
                {
                    _atEnd = true;
                    _isNextSet = false;
                }

                // If there is a peeked datom and it is marked as retraction then drop the pair.
                if (_isNextSet)
                {
                    var peekPrefix = _nextKeyCache.Ptr.Read<KeyPrefix>(0);
                    if (peekPrefix.IsRetract)
                    {
                        // Only filter out the pair if the retraction's timestamp is ≤ _txId.
                        if (peekPrefix.T <= _txId)
                        {
                            // Drop both by skipping to the next iteration.
                            _isNextSet = false;
                            continue;
                        }
                        else
                        {
                            // Otherwise, ignore the peeked retraction by clearing the flag.
                            _isNextSet = false;
                        }

                    }
                }

                // Otherwise, return the current datom.
                return true;
            }
        }

        public KeyPrefix KeyPrefix => _keyCache.Ptr.Read<KeyPrefix>(0);
        public Ptr Current => _keyCache.Ptr;
        public Ptr ValueSpan => _keyCache.Ptr.SliceFast(KeyPrefix.Size);
        public Ptr ExtraValueSpan => _extraCache.Ptr;
    }
}
