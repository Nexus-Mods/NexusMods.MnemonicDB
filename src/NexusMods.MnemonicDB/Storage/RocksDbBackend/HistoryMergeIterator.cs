using System;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

/// <summary>
/// A iterator that merges in the history datoms (TLeft is the history iterator)
/// </summary>
public struct HistoryMergeIterator<TLeft, TRight> : ILowLevelIterator 
    where TLeft : ILowLevelIterator 
    where TRight : ILowLevelIterator
{
    private readonly TLeft _left;
    private readonly TRight _right;

    // Cached state for the left iterator.
    private bool _leftIsValid;
    private Ptr _leftKey;
    private Ptr _leftValue;

    // Cached state for the right iterator.
    private bool _rightIsValid;
    private Ptr _rightKey;
    private Ptr _rightValue;

    // Cached comparison value between _leftKey and _rightKey.
    // Will be 0 if one of the iterators is invalid.
    private int _cmpCache = 0;

    public HistoryMergeIterator(TLeft left, TRight right)
    {
        _left = left;
        _right = right;
    }

    // Updates the left iterator's cached state.
    private void UpdateLeftCache()
    {
        _leftIsValid = _left.IsValid;
        if (_leftIsValid)
        {
            _leftKey = _left.Key;
            _leftValue = _left.Value;
        }
        UpdateCmpCache();
    }

    // Updates the right iterator's cached state.
    private void UpdateRightCache()
    {
        _rightIsValid = _right.IsValid;
        if (_rightIsValid)
        {
            _rightKey = _right.Key;
            _rightValue = _right.Value;
        }
        UpdateCmpCache();
    }

    // Updates the cached comparison value.
    // If both iterators are valid, computes _compare(_leftKey, _rightKey).
    // Otherwise, resets _cmpCache to null.
    private void UpdateCmpCache()
    {
        if (_leftIsValid && _rightIsValid)
        {
            _cmpCache = GlobalComparer.Compare(_leftKey, _rightKey);
        }
        else
        {
            _cmpCache = 0;
        }
    }

    /// <summary>
    /// Positions both underlying iterators to the provided span and caches their states.
    /// </summary>
    public void SeekTo(scoped ReadOnlySpan<byte> span)
    {
        _left.SeekTo(span);
        UpdateLeftCache();

        _right.SeekTo(span);
        UpdateRightCache();
    }

    /// <summary>
    /// Reverse iteration is not supported.
    /// </summary>
    public void SeekToPrev(scoped ReadOnlySpan<byte> span)
    {
        throw new NotSupportedException("Reverse iteration is not supported.");
    }

    /// <summary>
    /// The iterator is valid if either cached iterator is valid.
    /// </summary>
    public bool IsValid => _leftIsValid || _rightIsValid;

    /// <summary>
    /// Returns the current key from the underlying iterator with the lower key.
    /// Uses cached values and the cached comparison result.
    /// </summary>
    public Ptr Key
    {
        get
        {
            if (!IsValid)
                throw new InvalidOperationException("Iterator is not valid.");

            if (_leftIsValid && _rightIsValid)
            {
                if (_cmpCache == 0)
                    _cmpCache = GlobalComparer.Compare(_leftKey, _rightKey);
                return _cmpCache < 0 ? _leftKey : _rightKey;
            }
            else if (_leftIsValid)
            {
                return _leftKey;
            }
            else
            {
                return _rightKey;
            }
        }
    }

    /// <summary>
    /// Returns the current value corresponding to the current key.
    /// Uses the cached values.
    /// </summary>
    public Ptr Value
    {
        get
        {
            if (!IsValid)
                throw new InvalidOperationException("Iterator is not valid.");

            if (_leftIsValid && _rightIsValid)
            {
                if (_cmpCache == 0)
                    _cmpCache = GlobalComparer.Compare(_leftKey, _rightKey);
                return _cmpCache < 0 ? _leftValue : _rightValue;
            }
            else if (_leftIsValid)
            {
                return _leftValue;
            }
            else
            {
                return _rightValue;
            }
        }
    }

    /// <summary>
    /// Advances the iterator forward.
    /// It advances the underlying iterator with the lower key and updates its cache.
    /// </summary>
    public void Next()
    {
        if (!IsValid)
            return;

        if (_leftIsValid && _rightIsValid)
        {
            if (_cmpCache == 0)
                _cmpCache = GlobalComparer.Compare(_leftKey, _rightKey);

            if (_cmpCache < 0)
            {
                _left.Next();
                UpdateLeftCache();
            }
            else // _cmpCache.Value > 0 (duplicates are not possible)
            {
                _right.Next();
                UpdateRightCache();
            }
        }
        else if (_leftIsValid)
        {
            _left.Next();
            UpdateLeftCache();
        }
        else
        {
            _right.Next();
            UpdateRightCache();
        }
    }

    /// <summary>
    /// Reverse iteration is not supported.
    /// </summary>
    public void Prev()
    {
        throw new NotSupportedException("Reverse iteration is not supported.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _left.Dispose();
        _right.Dispose();
    }
}

