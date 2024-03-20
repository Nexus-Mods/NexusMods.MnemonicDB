using System;
using System.Buffers;
using Microsoft.Extensions.ObjectPool;

namespace NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;

public static class ExtensionMethods
{

    /// <summary>
    /// Start iterating over the datom result with a chunked iterator.
    /// </summary>
    public static DatomChunkIterator Iterate(this IDatomResult result)
    {
        return new DatomChunkIterator(DatomChunk.Create(), result);
    }

    /// <summary>
    /// A iterator for a chunked datom result.
    /// </summary>
    public struct DatomChunkIterator : IChunkIterator
    {
        private readonly DatomChunk _chunk;
        private readonly IDatomResult _result;
        private long _offset;
        private readonly long _length;

        internal DatomChunkIterator(DatomChunk chunk, IDatomResult result)
        {
            _chunk = chunk;
            _result = result;
            _offset = 0;
            _length = result.Length;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _chunk.Dispose();
        }

        /// <inheritdoc />
        public bool Next()
        {
            if (_offset >= _result.Length)
                return false;

            _chunk.Reset();
            _result.Fill(_offset, _chunk);

            if (_chunk.FilledDatoms + _offset > _length)
                _chunk.ApplyLimit((int)(_length - _offset));

            _offset += _chunk.FilledDatoms;
            return true;
        }

        /// <inheritdoc />
        public DatomChunk Current => _chunk;
    }
}
