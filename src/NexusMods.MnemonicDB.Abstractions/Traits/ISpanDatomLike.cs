using System;

namespace NexusMods.MnemonicDB.Abstractions.Traits;

public interface ISpanDatomLikeRO : IKeyPrefixLike
{
    public ReadOnlySpan<byte> ValueSpan { get; }
    public ReadOnlySpan<byte> ExtraValueSpan { get; }
}
