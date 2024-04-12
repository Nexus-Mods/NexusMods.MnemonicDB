using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Xunit.DependencyInjection;

namespace NexusMods.MnemonicDB.Storage.Tests;

public class KeyPrefixTests(ILogger<KeyPrefixTests> logger)
{
    [Fact]
    public void KeyPrefixValuesRoundTrip()
    {

        var data = from eid in new uint[] {0, 1, uint.MaxValue, uint.MaxValue / 2}
            from partition in new byte[] {0, 1, 14, 15}
            from type in new byte[] {0, 1, 14, 15}
            from isRetract in new[] {true, false}
            from txId in new uint[] {0, 1, uint.MaxValue, uint.MaxValue / 2}
            from attribute in new ushort[] {0, 1, ushort.MaxValue, ushort.MaxValue / 2}
            from length in new byte[] {0, 1, KeyPrefix.MaxLength, KeyPrefix.LengthOversized}
            select new {eid, partition, type, isRetract, txId, attribute, length};

        var total = 0;
        foreach (var row in data)
        {
            total++;
            var newPrefix = new KeyPrefix
            {
                E = EntityId.From(Ids.MakeId(row.partition, row.eid)),
                A = AttributeId.From(row.attribute),
                IsRetract = row.isRetract,
                T = TxId.From(row.txId),
                LowLevelType = (LowLevelTypes)row.type,
                ValueLength = row.length
            };

            var (eidOut, attributeOut, txIdOut, isRetractOut, lengthOut, typeOut) = newPrefix;
            eidOut.Should().Be(EntityId.From(Ids.MakeId(row.partition, row.eid)));
            ((byte)typeOut).Should().Be(row.type);
            isRetractOut.Should().Be(row.isRetract);
            txIdOut.Value.Should().Be(Ids.MakeId(Ids.Partition.Tx, row.txId));
            attributeOut.Should().Be(row.attribute);
            lengthOut.Should().Be(row.length);
        }

        logger.LogInformation("Total: {total} assertions", total);
    }

}
