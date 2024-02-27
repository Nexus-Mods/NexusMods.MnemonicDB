using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Datoms;
using NexusMods.EventSourcing.Storage.Serializers;

namespace NexusMods.EventSourcing.Storage.Tests;

public class AStorageTest
{
    protected readonly AttributeRegistry _registry;
    protected readonly NodeStore NodeStore;
    private readonly InMemoryKvStore _kvStore;

    public AStorageTest(IServiceProvider provider, IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes)
    {
        _registry = new AttributeRegistry(valueSerializers, attributes);
        _registry.Populate([
            new DbAttribute(Symbol.Intern<TestAttributes.FileHash>(), AttributeId.From(10), Symbol.Intern<UInt64Serializer>()),
            new DbAttribute(Symbol.Intern<TestAttributes.FileName>(), AttributeId.From(11), Symbol.Intern<StringSerializer>())
        ]);
        _kvStore = new InMemoryKvStore();
        NodeStore = new NodeStore(provider.GetRequiredService<ILogger<NodeStore>>(), _kvStore, Configuration.Default);
    }


    public IEnumerable<ITypedDatom> TestDatoms(ulong entityCount = 100)
    {
        var emitters = new Func<EntityId, TxId, ulong, ITypedDatom>[]
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
                    yield return emitters[a](EntityId.From(e), TxId.From(v), v);
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

    protected static void AssertEqual(in Datom rawDatomA, Datom datomB, int i)
    {
        rawDatomA.E.Should().Be(datomB.E, "at index " + i);
        rawDatomA.A.Should().Be(datomB.A, "at index " + i);
        rawDatomA.T.Should().Be(datomB.T, "at index " + i);
        rawDatomA.F.Should().Be(datomB.F, "at index " + i);
        rawDatomA.V.Span.SequenceEqual(datomB.V.Span).Should().BeTrue("at index " + i);
    }


    protected ITypedDatom Assert<TAttribute>(EntityId e, TxId tx, ulong value)
        where TAttribute : IAttribute<ulong>
    {
        return _registry.Datom<TAttribute, ulong>(e, tx, value);
    }

    protected ITypedDatom Assert<TAttribute>(EntityId e, TxId tx, string value)
        where TAttribute : IAttribute<string>
    {
        return _registry.Datom<TAttribute, string>(e, tx, value);
    }

}
