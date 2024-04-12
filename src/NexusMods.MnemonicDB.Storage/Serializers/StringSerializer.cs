using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Serializers;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Storage.Serializers;

/// <inheritdoc />
public class StringSerializer : AUtf8Serializer<String>
{
    protected override ReadOnlySpan<char> ToSpan(string value)
    {
        return value.AsSpan();
    }

    protected override string FromSpan(ReadOnlySpan<char> span)
    {
        return span.ToString();
    }

    public override Symbol UniqueId => Symbol.Intern<StringSerializer>();
}
