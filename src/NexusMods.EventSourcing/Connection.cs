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

    /// <summary>
    /// Main connection class, co-ordinates writes and immutable reads
    /// </summary>
    public Connection(IDatomStore store)
    {
        _store = store;

        AddMissingAttributes();
    }

    private void AddMissingAttributes()
    {
        var existing = ExistingAttributes().ToArray();

        Debug.Print(existing.Length.ToString());

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
            var uniqueId = UInt128.Zero;

            while (entIterator.Next())
            {
                var current = entIterator.Current;
                switch (current)
                {
                    case AssertDatom<BuiltInAttributes.ValueSerializerId, UInt128> serializerIdDatom:
                        serializerId = serializerIdDatom.V;
                        break;
                    case AssertDatom<BuiltInAttributes.UniqueId, UInt128> uniqueIdDatom:
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
