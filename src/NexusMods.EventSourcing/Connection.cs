using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.DatomStore;

namespace NexusMods.EventSourcing;

/// <summary>
/// Main connection class, co-ordinates writes and immutable reads
/// </summary>
public class Connection : IConnection
{
    private readonly IDatomStore _store;
    private readonly IAttribute[] _declaredAttributes;

    /// <summary>
    /// Main connection class, co-ordinates writes and immutable reads
    /// </summary>
    public Connection(IDatomStore store, IEnumerable<IAttribute> declaredAttributes, IEnumerable<IValueSerializer> serializers)
    {
        _store = store;
        _declaredAttributes = declaredAttributes.ToArray();

        AddMissingAttributes(serializers);
    }

    private void AddMissingAttributes(IEnumerable<IValueSerializer> valueSerializers)
    {
        var serializerByType = valueSerializers.ToDictionary(s => s.NativeType);

        var existing = ExistingAttributes().ToDictionary(a => a.UniqueId);

        var missing = _declaredAttributes.Where(a => !existing.ContainsKey(a.Id)).ToArray();
        if (missing.Length == 0)
            return;

        var datoms = new List<IDatom>();

        foreach (var (attr, id) in missing.Select((attribute, i) => (attribute, Ids.MakeId(Ids.Partition.Tmp, (ulong)i))))
        {
            var serializer = serializerByType[attr.ValueType];
            var uniqueId = attr.Id;
            datoms.Add(new AssertDatom<BuiltInAttributes.UniqueId, Symbol>(id, uniqueId));
            datoms.Add(new AssertDatom<BuiltInAttributes.ValueSerializerId, UInt128>(id, serializer.UniqueId));
        }
        _store.Transact(datoms);

    }

    private IEnumerable<DbAttribute> ExistingAttributes()
    {

        /*
         *  var attrs = from attrEnt in _db.EntsForAttr<BuiltInAttributes.UniqueId>()
         *              select _db.Pull<UniqueId, ValueSerializerId>(attrEnt);
         */


        var tx = TxId.MaxValue;
        var attrIterator = _store.Where<BuiltInAttributes.UniqueId>(tx);
        var entIterator = _store.EntityIterator(tx);
        while (attrIterator.Next())
        {
            entIterator.SetEntityId(attrIterator.EntityId);

            var serializerId = UInt128.Zero;
            Symbol uniqueId = null!;

            while (entIterator.Next())
            {
                var current = entIterator.Current;
                switch (current)
                {
                    case AssertDatom<BuiltInAttributes.ValueSerializerId, UInt128> serializerIdDatom:
                        serializerId = serializerIdDatom.V;
                        break;
                    case AssertDatom<BuiltInAttributes.UniqueId, Symbol> uniqueIdDatom:
                        uniqueId = uniqueIdDatom.V;
                        break;
                }
            }
            yield return new DbAttribute(uniqueId, attrIterator.EntityId.Value, serializerId);
        }

    }

    public IDb Db => new Db(_store, this, TxId);
    public TxId TxId { get; }
}
