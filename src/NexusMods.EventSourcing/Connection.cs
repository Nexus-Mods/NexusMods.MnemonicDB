using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing;

/// <summary>
/// Main connection class, co-ordinates writes and immutable reads
/// </summary>
public class Connection : IConnection
{
    private readonly object _lock = new();
    private ulong _nextEntityId = Ids.MinId(Ids.Partition.Entity);
    private readonly IDatomStore _store;
    internal readonly ModelReflector<Transaction> ModelReflector;
    private readonly Subject<ICommitResult> _updates;

    /// <summary>
    /// Main connection class, co-ordinates writes and immutable reads
    /// </summary>
    private Connection(IDatomStore store)
    {
        _store = store;
        ModelReflector = new ModelReflector<Transaction>(store);

        _updates = new Subject<ICommitResult>();

    }

    /// <summary>
    /// Creates and starts a new connection, some setup and reflection is done here so it is async
    /// </summary>
    public static async Task<Connection> Start(IDatomStore store, IEnumerable<IValueSerializer> serializers, IEnumerable<IAttribute> declaredAttributes)
    {
        var conn = new Connection(store);
        await conn.AddMissingAttributes(serializers, declaredAttributes);
        return conn;
    }


    private async Task AddMissingAttributes(IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> declaredAttributes)
    {
        var serializerByType = valueSerializers.ToDictionary(s => s.NativeType);

        var existing = ExistingAttributes().ToDictionary(a => a.UniqueId);

        var missing = declaredAttributes.Where(a => !existing.ContainsKey(a.Id)).ToArray();
        if (missing.Length == 0)
            return;

        var newAttrs = new List<DbAttribute>();

        var attrId = existing.Values.Max(a => a.AttrEntityId).Value;
        foreach (var attr in missing)
        {
            var id = ++attrId;

            var serializer = serializerByType[attr.ValueType];
            var uniqueId = attr.Id;
            newAttrs.Add(new DbAttribute(uniqueId, AttributeId.From(id), serializer.UniqueId));
        }

        await _store.RegisterAttributes(newAttrs);
    }

    private IEnumerable<DbAttribute> ExistingAttributes()
    {
        var attrIds = _store.Where<BuiltInAttributes.UniqueId>(TxId.MaxValue);

        foreach (var attr in attrIds)
        {
            var serializerId = Symbol.Unknown;
            var uniqueId = Symbol.Unknown;

            foreach (var datom in _store
                         .Where(TxId.MaxValue, attr.E)
                         .Typed(_store))
            {
                switch (datom)
                {
                    case BuiltInAttributes.ValueSerializerId.ReadDatom serializerIdDatom:
                        serializerId = serializerIdDatom.V;
                        break;
                    case BuiltInAttributes.UniqueId.ReadDatom uniqueIdDatom:
                        uniqueId = uniqueIdDatom.V;
                        break;
                }
            }
            yield return new DbAttribute(uniqueId, AttributeId.From(attr.E.Value), serializerId);
        }
    }


    /// <inheritdoc />
    public IDb Db => new Db(_store, this, TxId);


    /// <inheritdoc />
    public TxId TxId { get; private set; }


    /// <inheritdoc />
    public async Task<ICommitResult> Transact(IEnumerable<IWriteDatom> datoms)
    {
        var remaps = new Dictionary<ulong, ulong>();
        var datomsArray = datoms.ToArray();

        /*
        EntityId RemapFn(EntityId input)
        {
            if (Ids.GetPartition(input) == Ids.Partition.Tmp)
            {
                if (!remaps.TryGetValue(input.Value, out var id))
                {
                    var newId = _nextEntityId++;
                    remaps[input.Value] = newId;
                    return EntityId.From(newId);
                }
                return EntityId.From(id);
            }
            return input;
        }*/

        /*
        var newDatoms = new List<ITypedDatom>();
        foreach (var datom in datomsArray)
        {
            datom.Remap(RemapFn);
            newDatoms.Add(datom);
        }
        var newTx = await _store.Transact(newDatoms);
        TxId = newTx;
        var result = new CommitResult(newTx, remaps, datomsArray);
        _updates.OnNext(result);
        return result;
        */
        throw new NotImplementedException();
    }

    public Task<ICommitResult> Transact(IEnumerable<Datom> datoms)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ITransaction BeginTransaction()
    {
        return new Transaction(this);
    }

    /// <inheritdoc />
    public IObservable<ICommitResult> Commits => _updates;
}
