using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Datoms;
using NexusMods.EventSourcing.Storage.Serializers;

namespace NexusMods.EventSourcing.Storage.Tests;

public class AStorageTest
{
    protected readonly AttributeRegistry _registry;

    public AStorageTest(IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes)
    {
        _registry = new AttributeRegistry(valueSerializers, attributes);
        _registry.Populate([
            new DbAttribute(Symbol.Intern<TestAttributes.FileHash>(), 10, Symbol.Intern<UInt64Serializer>()),
            new DbAttribute(Symbol.Intern<TestAttributes.FileName>(), 11, Symbol.Intern<StringSerializer>())
        ]);
    }


    public IEnumerable<IRawDatom> TestDatoms(ulong entityCount = 100)
    {
        var emitters = new Func<ulong, ulong, ulong, IRawDatom>[]
        {
            (e, tx, v) => Assert<TestAttributes.FileHash>(e, tx, v),
            (e, tx, v) => Assert<TestAttributes.FileName>(e, tx, "file " + v),
        };

        for (ulong e = 0; e < entityCount; e++)
        {
            for (var a = 0; a < 2; a ++)
            {
                for (ulong v = 0; v < 3; v++)
                {
                    yield return emitters[a](e, v, v);
                }
            }
        }
    }

    protected static void AssertEqual(in IRawDatom rawDatomA, IRawDatom datomB, int i)
    {
        rawDatomA.EntityId.Should().Be(datomB.EntityId, "at index " + i);
        rawDatomA.AttributeId.Should().Be(datomB.AttributeId, "at index " + i);
        rawDatomA.TxId.Should().Be(datomB.TxId, "at index " + i);
        rawDatomA.Flags.Should().Be(datomB.Flags, "at index " + i);
        rawDatomA.ValueLiteral.Should().Be(datomB.ValueLiteral, "at index " + i);
        rawDatomA.ValueSpan.SequenceEqual(datomB.ValueSpan).Should().BeTrue("at index " + i);
    }


    protected OnHeapDatom Assert<TAttribute>(ulong e, ulong tx, ulong value)
        where TAttribute : IAttribute<ulong>
    {
        return _registry.Datom<TAttribute, ulong>(e, tx, value);
    }

    protected OnHeapDatom Assert<TAttribute>(ulong e, ulong tx, string value)
        where TAttribute : IAttribute<string>
    {
        return _registry.Datom<TAttribute, string>(e, tx, value);
    }

}
