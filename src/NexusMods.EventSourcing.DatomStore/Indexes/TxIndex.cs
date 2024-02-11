namespace NexusMods.EventSourcing.DatomStore.Indexes;

public class TxIndex(AttributeRegistry registry) : AIndexDefinition(registry, "txLog")
{
    public override unsafe int Compare(KeyHeader* a, uint aLength, KeyHeader* b, uint bLength)
    {
        // TX, Entity, Attribute, IsAssert, Value
        var cmp = KeyHeader.CompareTx(a, b);
        if (cmp != 0) return cmp;
        cmp = KeyHeader.CompareEntity(a, b);
        if (cmp != 0) return cmp;
        cmp = KeyHeader.CompareAttribute(a, b);
        if (cmp != 0) return cmp;
        cmp = KeyHeader.CompareIsAssert(a, b);
        if (cmp != 0) return cmp;
        return KeyHeader.CompareValues(Registry, a, aLength, b, bLength);
    }
}
