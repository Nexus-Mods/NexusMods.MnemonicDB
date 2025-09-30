using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.QueryFunctions;

public class DatomsTableFunction : ATableFunction, IRevisableFromAttributes
{
    private readonly string _prefix;
    private readonly QueryEngine _queryEngine;
    
    private readonly ConcurrentDictionary<IConnection, FrozenDictionary<AttributeId, ushort>> _attributeToEnumMappings = new();
    
    public DatomsTableFunction(IQueryEngine queryEngine, string prefix = "mdb")
    {
        _prefix = prefix;
        _queryEngine = (QueryEngine)queryEngine;
    }
    
    protected override void Setup(RegistrationInfo info)
    {
        info.SetName($"{_prefix}_Datoms");
        info.AddNamedParameter<string>("A");
        info.AddNamedParameter<bool>("History");
        info.AddNamedParameter<ulong>("Db");
        info.SupportsPredicatePushdown();
    }

    protected override void Execute(FunctionInfo functionInfo)
    {
        var state = functionInfo.GetBindInfo<LocalBindData>();
        var localInitData = functionInfo.GetInitInfo<LocalInitData>();
        if (state.Mode == ScanMode.AllAttributes)
            ExecuteAllAttributes(functionInfo, state, localInitData);
        else
        {
            ExecuteOneAttribute(functionInfo, state, localInitData);
        }
    }

    private void ExecuteOneAttribute(FunctionInfo functionInfo, LocalBindData state, LocalInitData initData)
    {

        var chunk = functionInfo.Chunk;
        var eVec = functionInfo.GetWritableVector(0).GetData<ulong>();
        var vVec = functionInfo.GetWritableVector(1);
        var vData = vVec.GetData();
        var tVec = functionInfo.GetWritableVector(2).GetData<ulong>();
        var lowLevelType = state.Attribute!.LowLevelType;
        
        Span<byte> isRetract = Span<byte>.Empty;
        
        if (state.History)
            isRetract = functionInfo.GetWritableVector(3).GetData<byte>();
        
        var iterator = initData.Segment;
        var row = 0;
        while (iterator.MoveNext())
        {
            if (!eVec.IsEmpty)
                eVec[row] = iterator.KeyPrefix.E.Value;
            if (!tVec.IsEmpty) 
                tVec[row] = iterator.KeyPrefix.T.Value;
            if (state.History && !isRetract.IsEmpty)
                isRetract[row] = iterator.KeyPrefix.IsRetract ? (byte)1 : (byte)0;
            
            if (vVec.IsValid)
            {
                switch (lowLevelType)
                {
                    case ValueTag.UInt8:
                        vData.CastFast<byte, byte>()[row] = iterator.ValueSpan[0];
                        break;
                    case ValueTag.UInt16:
                        vData.CastFast<byte, ushort>()[row] = iterator.ValueSpan.CastFast<byte, ushort>()[0];
                        break;
                    case ValueTag.UInt32:
                        vData.CastFast<byte, uint>()[row] = iterator.ValueSpan.CastFast<byte, uint>()[0];
                        break;
                    case ValueTag.UInt64:
                        vData.CastFast<byte, ulong>()[row] = UInt64Serializer.Read(iterator.ValueSpan);
                        break;
                    case ValueTag.UInt128:
                        vData.CastFast<byte, UInt128>()[row] = UInt128Serializer.Read(iterator.ValueSpan);
                        break;
                    case ValueTag.Int16:
                        vData.CastFast<byte, short>()[row] = iterator.ValueSpan.CastFast<byte, short>()[0];
                        break;
                    case ValueTag.Int32:
                        vData.CastFast<byte, int>()[row] = iterator.ValueSpan.CastFast<byte, int>()[0];
                        break;
                    case ValueTag.Int64:
                        vData.CastFast<byte, long>()[row] = iterator.ValueSpan.CastFast<byte, long>()[0];
                        break;
                    case ValueTag.Int128:
                        vData.CastFast<byte, Int128>()[row] = Int128Serializer.Read(iterator.ValueSpan);
                        break;
                    case ValueTag.Float32:
                        vData.CastFast<byte, float>()[row] = iterator.ValueSpan.CastFast<byte, float>()[0];
                        break;
                    case ValueTag.Float64:
                        vData.CastFast<byte, double>()[row] = iterator.ValueSpan.CastFast<byte, double>()[0];
                        break;
                    case ValueTag.Ascii:
                        vVec.WriteUtf8((ulong)row, iterator.ValueSpan);
                        break;
                    case ValueTag.Utf8:
                        vVec.WriteUtf8((ulong)row, iterator.ValueSpan);
                        break;
                    case ValueTag.Utf8Insensitive:
                        vVec.WriteUtf8((ulong)row, iterator.ValueSpan);
                        break;
                    case ValueTag.Reference:
                        vData.CastFast<byte, ulong>()[row] = iterator.ValueSpan.CastFast<byte, ulong>()[0];
                        break;
                    default:
                        throw new NotImplementedException($"Not implemented for {lowLevelType}");
                }
            }

            row++;
            if (row >= eVec.Length)
                break;
        }
        
        chunk.Size = (ulong)row;
    }

    private void ExecuteAllAttributes(FunctionInfo functionInfo, LocalBindData state, LocalInitData initData)
    {
        var chunk = functionInfo.Chunk;
        var eVec = functionInfo.GetWritableVector(0).GetData<ulong>();
        var aVec = functionInfo.GetWritableVector(1).GetData<byte>();
        var vVec = functionInfo.GetWritableVector(2);
        var tVec = functionInfo.GetWritableVector(3).GetData<ulong>();
        var vTagVec = functionInfo.GetWritableVector(4).GetData<byte>();
        var historyVector = functionInfo.GetWritableVector(5).GetData<byte>();
        var mappings = initData.Mappings;

        var iterator = initData.Segment;
        int row = 0;
        int width = _queryEngine.AttrEnumWidth;
        
        while (iterator.MoveNext())
        {
            if (!eVec.IsEmpty)
                eVec[row] = iterator.KeyPrefix.E.Value;
            if (!aVec.IsEmpty)
            {
                if (!mappings.TryGetValue(iterator.KeyPrefix.A, out var value))
                    value = 0; // Translates to "UNKNOWN"

                if (width == 1)
                    aVec[row] = (byte)value;
                if (width == 2)
                    aVec.CastFast<byte, ushort>()[row] = value;
            }

            if (!tVec.IsEmpty) 
                tVec[row] = iterator.KeyPrefix.T.Value;
            
            if (vVec.IsValid)
                vVec.WriteBlob((ulong)row, iterator.ValueSpan);

            if (!vTagVec.IsEmpty)
                vTagVec[row] = (byte)iterator.KeyPrefix.ValueTag;
            if (!historyVector.IsEmpty)
                historyVector[row] = (byte)(iterator.KeyPrefix.IsRetract ? 1 : 0);
                
            row++;
            if (row >= eVec.Length)
                break;
        }
        chunk.Size = (ulong)row;
    }

    protected override void Bind(BindInfo info)
    {
        var aParam = info.GetParameter("A");
        var historyParam = info.GetParameter("History");
        var history = !historyParam.IsNull && historyParam.GetBool();
        
        var dbParam = info.GetParameter("Db");
        IConnection connection;
        TxId asOf;
        if (!dbParam.IsNull)
        {
            var db = dbParam.GetUInt64();
            var dbAndAsOf = dbParam.GetUInt64();
            asOf = TxId.From(PartitionId.Transactions.MakeEntityId(dbAndAsOf >> 16).Value);
            connection = _queryEngine.GetConnectionByUid((ushort)(dbAndAsOf & 0xFFFF))!;
        }
        else
        {
            asOf = TxId.MinValue;
            connection = _queryEngine.GetConnectionByName()!;
        }
        
        LocalBindData state;
        if (!aParam.IsNull)
        {
            var attrToFind = aParam.GetVarChar();
            if (!_queryEngine.AttributesByShortName.TryGetValue(attrToFind, out var attr))
                throw new Exception($"Attribute '{attrToFind}' not found");

            state = new LocalBindData
            {
                Connection = connection,
                AsOf = asOf,
                Mode = ScanMode.SingleAttribute,
                Attribute = attr,
            };
        }
        else
        {
            state = new LocalBindData
            {
                Connection = connection,
                AsOf = asOf,
                Mode = ScanMode.AllAttributes,
                Attribute = null
            };
        }

        if (state.Mode == ScanMode.SingleAttribute)
        {
            info.AddColumn<ulong>("E");
            info.AddColumn("V", state.Attribute!.LowLevelType.DuckDbType(state.Attribute.ValueType));
            info.AddColumn<ulong>("T");
        }
        else
        {
            info.AddColumn<ulong>("E");
            info.AddColumn("A", _queryEngine.AttrEnum);
            info.AddColumn<byte[]>("V");
            info.AddColumn<ulong>("T");
            info.AddColumn("ValueTag", ValueTagsExtensions.ValueTagsEnum);
        }
        
        state.History = history;
        if (history)
            info.AddColumn<bool>("IsRetract");
        info.SetBindInfo(state);
    }

    protected override object? Init(InitInfo initInfo, InitData initData)
    {
        var bindData = initInfo.GetBindData<LocalBindData>();
        IDb db;
        if (bindData.AsOf == TxId.MinValue)
            db = bindData.Connection.Db;
        else
            db = bindData.Connection.AsOf(bindData.AsOf);
        if (bindData.History)
            db = bindData.Connection.History();

        var mappings = GetAttrIdToEnumMappings(db.Connection);
        
        if (bindData.Mode == ScanMode.SingleAttribute)
        {
            var aid = AttributeId.Max;
            // This attribute may not exist in the database, if it doesn't we'll use the max Id so we return no data
            if (db.AttributeCache.TryGetAttributeId(bindData.Attribute!.Id, out var foundId))
                aid = foundId;
            
            return new LocalInitData
            {
                Segment = db.LightweightDatoms(SliceDescriptor.Create(aid)),
                Mappings = mappings,
            };
        }
        else
        {
            return new LocalInitData
            {
                Segment = db.LightweightDatoms(SliceDescriptor.AllEntities(PartitionId.Entity), totalOrdered: true),
                Mappings = mappings,
            };
        }
        
    }

    private FrozenDictionary<AttributeId, ushort> GetAttrIdToEnumMappings(IConnection dbConnection)
    {
        if (_attributeToEnumMappings.TryGetValue(dbConnection, out var mappings))
            return mappings;
        
        var cache = dbConnection.AttributeCache;
        var dict = new Dictionary<AttributeId, ushort>();
        foreach (var entry in _queryEngine.AttrEnumEntries)
        {
            if (entry.EnumId == 0)
                // This is the "UNKNOWN" entry
                continue;
            
            // Not all attributes in code will have a match in this database
            if (!cache.TryGetAttributeId(entry.Attribute.Id, out var foundId))
                continue;

            dict.Add(foundId, entry.EnumId);
        }

        var frozen = dict.ToFrozenDictionary();
        _attributeToEnumMappings[dbConnection] = frozen;
        return frozen;
    }

    private enum ScanMode
    {
        SingleAttribute,
        AllAttributes,
    }

    private class LocalBindData
    {
        public required IConnection Connection;
        public required TxId AsOf;
        public bool History = false;
        public required ScanMode Mode;
        public required IAttribute? Attribute;
    }

    private class LocalInitData
    {
        public required ILightweightDatomSegment Segment;
        public required FrozenDictionary<AttributeId, ushort> Mappings;
    }

    public void ReviseFromAttrs(IReadOnlySet<IAttribute> attrs)
    {
        Revise();
    }
}
