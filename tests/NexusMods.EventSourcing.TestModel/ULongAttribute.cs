using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel;

public class ULongAttribute<TOwner>(string attrName) :
    ScalarAttribute<TOwner, ulong>(attrName),
    IIndexableAttribute<ulong>
    where TOwner : AEntity
{
    public void WriteTo(Span<byte> span, ulong value)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(span, value);
    }

    public bool Equal(IAccumulator accumulator, ulong val)
    {
        return ((ScalarAccumulator<ulong>)accumulator).Value == val;
    }

    public UInt128 IndexedAttributeId { get; set; }
    public int SpanSize()
    {
        return 8;
    }

    public void WriteTo(Span<byte> span, IAccumulator accumulator)
    {
        WriteTo(span, ((ScalarAccumulator<ulong>)accumulator).Value);
    }
}
