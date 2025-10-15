using System;

namespace NexusMods.MnemonicDB.Abstractions.Traits;

public interface ISpanDatomLikeRO : IKeyPrefixLikeRO
{
    public ReadOnlySpan<byte> ValueSpan { get; }
    public ReadOnlySpan<byte> ExtraValueSpan { get; }
}
