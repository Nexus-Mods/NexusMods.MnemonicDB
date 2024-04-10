using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TransparentValueObjects;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// It's extremely rare (basically only during migrations) that two attribute registry instances will exist
/// at one time. But it's possible, so we'll assign a unique identifier to each attribute registry, and use
/// this key to reference the attribute registry caches
/// </summary>
[ValueObject<byte>]
public readonly partial struct RegistryId
{


    [InlineArray(MaxSize)]
    public struct InlineCache
    {
        /// <summary>
        /// The maximum number of attributes that can be stored in the cache
        /// </summary>
        public const int MaxSize = 8;

        private AttributeId _registryId;
    }
}
