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
            new DbAttribute(Symbol.Intern<TestAttributes.FileHash>(), 10, Symbol.Intern<UInt64Serializer>())
        ]);
    }


    protected OnHeapDatom Assert<TAttribute>(ulong e, ulong tx, ulong value)
        where TAttribute : IAttribute<ulong>
    {
        return _registry.Datom<TAttribute, ulong>(e, value, tx);
    }

}
