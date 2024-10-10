using System.Buffers;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Storage.Tests.TestAttributes;

public class Blobs
{
    private const string Namespace = "NexusMods.MnemonicDB.Storage.Tests.TestAttributes";

    public static readonly TestBlobAttribute InKeyBlob = new(Namespace, "InKeyBlob") {IsIndexed = true};
    public static readonly TestHashedBlobAttribute InValueBlob = new(Namespace, "InValueBlob") {IsIndexed = true};
}


public sealed class TestBlobAttribute(string ns, string name) : ScalarAttribute<Memory<byte>, Memory<byte>>(ValueTag.Blob, ns, name)
{
    protected override Memory<byte> ToLowLevel(Memory<byte> value) => value;

    protected override Memory<byte> FromLowLevel(Memory<byte> value, AttributeResolver resolver) => value;
}

public class TestHashedBlobAttribute(string ns, string name) : ScalarAttribute<Memory<byte>, Memory<byte>>(ValueTag.HashedBlob, ns, name)
{
    protected override Memory<byte> ToLowLevel(Memory<byte> value) => value;

    protected override Memory<byte> FromLowLevel(Memory<byte> value, AttributeResolver resolver) => value;
}
