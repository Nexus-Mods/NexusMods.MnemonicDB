using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Query;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.QueryFunctions;

public class ModelTableFunction : ATableFunction, IRevisableFromAttributes
{
    private readonly ModelDefinition _definition;
    private readonly QueryEngine _engine;
    private readonly string _prefix;
    
    public ModelTableFunction(QueryEngine engine, ModelDefinition definition, string prefix = "mdb")
    {
        _prefix = prefix;
        _engine = engine;
        _definition = definition;
    }

    /// <summary>
    /// Possibly revise the function if it depends on any of the changed attributes
    /// </summary>
    public void ReviseFromAttrs(IReadOnlySet<IAttribute> attrs)
    {
        if (attrs.Overlaps(_definition.AllAttributes))
            Revise();
    }
    
    protected override void Setup(RegistrationInfo info)
    {
        info.SetName($"{_prefix}_{_definition.Name}");
        info.SupportsPredicatePushdown();
        info.AddNamedParameter<ulong>("AsOf");
        info.AddNamedParameter<ulong>("Db");
        info.AddNamedParameter<string>("DbName");
    }

    protected override void Execute(FunctionInfo functionInfo)
    {
        var initData = functionInfo.GetInitInfo<LocalInitData>();
        var bindData = functionInfo.GetBindInfo<BindData>();
        var outChunk = functionInfo.Chunk;
        if (!initData.NextChunk(out var idsChunk, out var emitSize))
        {
            outChunk.Size = 0;
            return;
        }

        outChunk.Size = (ulong)emitSize;
        if (emitSize == 0)
            return;

        var idVec = idsChunk.AsSpan().SliceFast(0, emitSize);
        
        var engineToFn = functionInfo.EngineToFn;
        // Now write the other columns
        for (var columnIdx = 0; columnIdx < engineToFn.Length; columnIdx++)
        {
            var mapping = engineToFn[columnIdx];
            // Id Column, no reason to iterate, copy over the ids
            if (mapping == 0)
            {
                var data = outChunk[(ulong)columnIdx].GetData<EntityId>();
                idVec.CopyTo(data);
                continue;
            }
            
            var attr = _definition.AllAttributes[mapping - 1];
            var attrId = initData.Db.AttributeCache.GetAttributeId(attr.Id);
            var iterator = initData.Db.LightweightDatoms(SliceDescriptor.AttributesStartingAt(attrId, idVec[0]));
            var valueVector = outChunk[(ulong)columnIdx];
            if (attr.Cardinalty == Cardinality.Many)
                WriteMultiCardinality(attr.LowLevelType, idVec, valueVector, iterator);
            else
                WriteColumn(attr.LowLevelType, attr.ValueType, idVec, valueVector, iterator);
        }
        ArrayPool<EntityId>.Shared.Return(idsChunk);
    }

    private void WriteMultiCardinality<TVector>(ValueTag attrLowLevelType, Span<EntityId> ids, TVector valueVector, ILightweightDatomSegment datoms)
        where TVector : IWritableVector, allows ref struct
    {
        switch (attrLowLevelType)
        {
            case ValueTag.UInt8:
                WriteMultiCardinalityCore<TVector, byte>(attrLowLevelType, ids, valueVector, datoms);
                break;
            case ValueTag.UInt16:
                WriteMultiCardinalityCore<TVector, ushort>(attrLowLevelType, ids, valueVector, datoms);
                break;
            case ValueTag.UInt32:
                WriteMultiCardinalityCore<TVector, uint>(attrLowLevelType, ids, valueVector, datoms);
                break;
            case ValueTag.UInt64:
                WriteMultiCardinalityCore<TVector, ulong>(attrLowLevelType, ids, valueVector, datoms);
                break;
            case ValueTag.UInt128:
                WriteMultiCardinalityCore<TVector, UInt128>(attrLowLevelType, ids, valueVector, datoms);
                break;
            case ValueTag.Int16:
                WriteMultiCardinalityCore<TVector, short>(attrLowLevelType, ids, valueVector, datoms);
                break;
            case ValueTag.Int32:
                WriteMultiCardinalityCore<TVector, int>(attrLowLevelType, ids, valueVector, datoms);
                break;
            case ValueTag.Int64:
                WriteMultiCardinalityCore<TVector, long>(attrLowLevelType, ids, valueVector, datoms);
                break;
            case ValueTag.Int128:
                WriteMultiCardinalityCore<TVector, Int128>(attrLowLevelType, ids, valueVector, datoms);
                break;
            case ValueTag.Float32:
                WriteMultiCardinalityCore<TVector, float>(attrLowLevelType, ids, valueVector, datoms);
                break;
            case ValueTag.Float64:
                WriteMultiCardinalityCore<TVector, double>(attrLowLevelType, ids, valueVector, datoms);
                break;
            case ValueTag.Ascii:
                WriteMultiCardinalityCore<TVector, StringElement>(attrLowLevelType, ids, valueVector, datoms);
                break;
            case ValueTag.Utf8Insensitive:
                WriteMultiCardinalityCore<TVector, StringElement>(attrLowLevelType, ids, valueVector, datoms);
                break;
            case ValueTag.Utf8:
                WriteMultiCardinalityCore<TVector, StringElement>(attrLowLevelType, ids, valueVector, datoms);
                break;
            case ValueTag.Reference:
                WriteMultiCardinalityCore<TVector, ulong>(attrLowLevelType, ids, valueVector, datoms);
                break;
            default:
                throw new NotImplementedException("Not implemented for list vector of " + attrLowLevelType);
        }
    }

    private void WriteMultiCardinalityCore<TVector, TType>(ValueTag attrLowLevelType, Span<EntityId> ids, TVector valueVector, ILightweightDatomSegment datoms)
        where TVector : IWritableVector, allows ref struct 
        where TType : unmanaged
    {
        using var writer = new ListWriter<TVector, TType>(valueVector);

        for (var rowIdx = 0; rowIdx < ids.Length; rowIdx++)
        {
            var rowId = ids[rowIdx];
            if (datoms.FastForwardTo(rowId))
            {
                writer.SetStart();
                while (datoms.KeyPrefix.E.Value == rowId)
                {
                    if (typeof(TType) == typeof(StringElement))
                    {
                        writer.WriteUtf8(datoms.ValueSpan);
                    }
                    else
                    {
                        writer.Write(datoms.ValueSpan);
                    }
                    
                    if (!datoms.MoveNext())
                        break;
                }

                writer.WriteCurrentEntry();
            }
            else
            {
                writer.WriteCurrentEntry();
            }
        }
    }

    private void WriteColumn<TVector>(ValueTag tag, Type type, ReadOnlySpan<EntityId> ids, TVector vector, ILightweightDatomSegment datoms) where TVector : IWritableVector, allows ref struct
    {
        switch (tag)
        {
            case ValueTag.Null:
                WriteNullColumn(ids, vector, datoms);
                break;
            case ValueTag.UInt8:
                WriteUnmanagedColumn<TVector, byte>(ids, vector, datoms);
                break;
            case ValueTag.UInt16:
                WriteUnmanagedColumn<TVector, ushort>(ids, vector, datoms);
                break;
            case ValueTag.UInt32:
                WriteUnmanagedColumn<TVector, uint>(ids, vector, datoms);
                break;
            case ValueTag.UInt64:
                WriteUnmanagedColumn<TVector, ulong>(ids, vector, datoms);
                break;
            case ValueTag.UInt128:
                WriteUnmanagedColumn<TVector, UInt128>(ids, vector, datoms);
                break;
            case ValueTag.Int16:
                WriteUnmanagedColumn<TVector, short>(ids, vector, datoms);
                break;
            case ValueTag.Int32:
                WriteUnmanagedColumn<TVector, int>(ids, vector, datoms);
                break;
            case ValueTag.Int64:
            {
                if (type == typeof(DateTimeOffset)) WriteDateTimeOffset(ids, vector, datoms);
                else WriteUnmanagedColumn<TVector, long>(ids, vector, datoms);
                break;
            }
            case ValueTag.Int128:
                WriteUnmanagedColumn<TVector, Int128>(ids, vector, datoms);
                break;
            case ValueTag.Float32:
                WriteUnmanagedColumn<TVector, float>(ids, vector, datoms);
                break;
            case ValueTag.Float64:
                WriteUnmanagedColumn<TVector, double>(ids, vector, datoms);
                break;
            case ValueTag.Ascii:
                WriteVarCharColumn(ids, vector, datoms);
                break;
            case ValueTag.Utf8Insensitive:
                WriteVarCharColumn(ids, vector, datoms);
                break;
            case ValueTag.Utf8:
                WriteVarCharColumn(ids, vector, datoms);
                break;
            case ValueTag.Reference:
                WriteUnmanagedColumn<TVector, ulong>(ids, vector, datoms);
                break;
            case ValueTag.Tuple2_UShort_Utf8I:
                WriteTuple2Column(ids, vector, datoms);
                break;
            case ValueTag.Tuple3_Ref_UShort_Utf8I:
                WriteTuple3Column(ids, vector, datoms);
                break;
            default:
                throw new NotImplementedException("Not implemented for " + tag);
        }
    }

    private void WriteNullColumn<TVector>(ReadOnlySpan<EntityId> ids, TVector vector, ILightweightDatomSegment datoms) 
        where TVector : IWritableVector, allows ref struct
    {
        var dataSpan = vector.GetData<byte>();
        for (var rowIdx = 0; rowIdx < ids.Length; rowIdx++)
        {
            var rowId = ids[rowIdx];
            if (datoms.FastForwardTo(rowId))
            {
                dataSpan[rowIdx] = 1;
            }
            else
            {
                dataSpan[rowIdx] = 0;
            }
        }
    }

    private void WriteTuple3Column<TVector>(ReadOnlySpan<EntityId> ids, TVector vector, ILightweightDatomSegment datoms)
        where TVector : IWritableVector, allows ref struct
    {
        var validityMask = vector.GetValidityMask();
        var refVector = vector.GetStructChild(0).GetData<ulong>();
        var shortVector = vector.GetStructChild(1).GetData<ushort>();
        var stringVector = vector.GetStructChild(2);
        for (var rowIdx = 0; rowIdx < ids.Length; rowIdx++)
        {
            var rowId = ids[rowIdx];
            if (datoms.FastForwardTo(rowId))
            {
                refVector[rowIdx] = datoms.ValueSpan.CastFast<byte, ulong>()[0];
                shortVector[rowIdx] = datoms.ValueSpan.SliceFast(sizeof(ulong)).CastFast<byte, ushort>()[0];
                stringVector.WriteUtf8((ulong)rowIdx, datoms.ValueSpan.SliceFast(sizeof(ulong) + sizeof(ushort)));
                validityMask[(ulong)rowIdx] = true;
            }
            else
            {
                validityMask[(ulong)rowIdx] = false;
            }
        }
    }

    private void WriteTuple2Column<TVector>(ReadOnlySpan<EntityId> ids, TVector vector, ILightweightDatomSegment datoms) 
        where TVector : IWritableVector, allows ref struct
    {
        var validityMask = vector.GetValidityMask();
        var shortVector = vector.GetStructChild(0).GetData<ushort>();
        var stringVector = vector.GetStructChild(1);
        for (var rowIdx = 0; rowIdx < ids.Length; rowIdx++)
        {
            var rowId = ids[rowIdx];
            if (datoms.FastForwardTo(rowId))
            {
                shortVector[rowIdx] = datoms.ValueSpan.CastFast<byte, ushort>()[0];
                stringVector.WriteUtf8((ulong)rowIdx, datoms.ValueSpan.SliceFast(sizeof(ushort)));
                validityMask[(ulong)rowIdx] = true;
            }
            else
            {
                validityMask[(ulong)rowIdx] = false;
            }
        }
    }

    private static void WriteDateTimeOffset<TVector>(ReadOnlySpan<EntityId> ids, TVector vector, ILightweightDatomSegment datoms)
        where TVector : IWritableVector, allows ref struct
    {
        var validityMask = vector.GetValidityMask();
        var dataSpan = vector.GetData<long>();
        for (var rowIdx = 0; rowIdx < ids.Length; rowIdx++)
        {
            var rowId = ids[rowIdx];
            if (datoms.FastForwardTo(rowId))
            {
                // NOTE(erri120): TIMESTAMP_NS is stored as nanoseconds since 1970-01-01 in int64_t.
                var ticks = datoms.ValueSpan.CastFast<byte, long>()[0];
                var dateTimeOffset = new DateTimeOffset(ticks, offset: TimeSpan.Zero);
                var timeSpan = dateTimeOffset - DateTimeOffset.UnixEpoch;
                var nanoseconds = (long)timeSpan.TotalNanoseconds;

                dataSpan[rowIdx] = nanoseconds;
                validityMask[(ulong)rowIdx] = true;
            }
            else
            {
                validityMask[(ulong)rowIdx] = false;
            }
        }
    }
    
    private static void WriteUnmanagedColumn<TVector, T>(ReadOnlySpan<EntityId> ids, TVector vector, ILightweightDatomSegment datoms) 
        where TVector : IWritableVector, allows ref struct
        where T : unmanaged
    {
        var validityMask = vector.GetValidityMask();
        var dataSpan = vector.GetData<T>();
        for (var rowIdx = 0; rowIdx < ids.Length; rowIdx++)
        {
            var rowId = ids[rowIdx];
            if (datoms.FastForwardTo(rowId))
            {
                dataSpan[rowIdx] = datoms.ValueSpan.CastFast<byte, T>()[0];
                validityMask[(ulong)rowIdx] = true;
            }
            else
            {
                validityMask[(ulong)rowIdx] = false;
            }
        }
    }

    private void WriteVarCharColumn<TVector>(ReadOnlySpan<EntityId> ids, TVector vector, ILightweightDatomSegment datoms)
        where TVector : IWritableVector, allows ref struct
    {
        var mask = vector.GetValidityMask();
        for (var rowIdx = 0; rowIdx < ids.Length; rowIdx++)
        {
            var rowId = ids[rowIdx];
            if (datoms.FastForwardTo(rowId))
            {
                vector.WriteUtf8((ulong)rowIdx, datoms.ValueSpan);
                mask[(ulong)rowIdx] = true;
            }
            else
            {
                mask[(ulong)rowIdx] = false;
            }
        }
    }

    private class LocalInitData
    {
        public int NextId;
        public required IDb Db;
        public required List<EntityId[]> Ids;
        public required int RowCount;
        public required int ChunkSize;

        public bool NextChunk(out EntityId[] ids, out int emitSize)
        {
            var thisId = Interlocked.Increment(ref NextId) - 1;
            if (thisId >= Ids.Count)
            {
                ids = [];
                emitSize = 0;
                return false;
            }
            ids = Ids[thisId];
            if (thisId == Ids.Count - 1)
                emitSize = RowCount - (Ids.Count - 1) * ChunkSize;
            else 
                emitSize = ids.Length;
            return true;
        }

    }

    protected override object? Init(InitInfo initInfo, InitData initData)
    {
        var bindInfo = initInfo.GetBindData<BindData>();
        IDb db;
        // Asof 0 means "always use latest"
        if (bindInfo.AsOf == TxId.MinValue)
            db = bindInfo.Connection.Db;
        else 
            db = bindInfo.Connection.AsOf(bindInfo.AsOf);
            
        // Load all the Ids here so we can load the rows in parallel.
        var totalRows = db.IdsForPrimaryAttribute(bindInfo.PrimaryAttributeId, (int)GlobalConstants.DefaultVectorSize, out var chunks);
        initInfo.SetMaxThreads(chunks.Count);
        return new LocalInitData
        {
            Db = db,
            NextId = 0,
            Ids = chunks,
            RowCount = totalRows,
            ChunkSize = (int)GlobalConstants.DefaultVectorSize,
        };
    }

    private class BindData
    {
        public required TxId AsOf;
        public AttributeId PrimaryAttributeId;
        public required IConnection Connection;
    }
    protected override void Bind(BindInfo info)
    {
        info.AddColumn<ulong>("Id");
        foreach (var attr in _definition.AllAttributes)
        {
            if (attr.Cardinalty == Cardinality.Many)
            {
                var baseType = attr.LowLevelType.DuckDbType(attr.ValueType);
                using var listType = LogicalType.CreateListOf(baseType);
                info.AddColumn(attr.Id.Name, listType);
            }
            else
            {
                info.AddColumn(attr.Id.Name, attr.LowLevelType.DuckDbType(attr.ValueType));
            }
        }

        using var dbParam = info.GetParameter("Db");
        using var asOfParam = info.GetParameter("AsOf");
        using var dbNameParam = info.GetParameter("DbName");
        TxId? asOf = null;
        IConnection conn;

        if (dbParam.IsNull && dbNameParam.IsNull)
        {
            conn = _engine.GetConnectionByName()!;
        }
        else if (!dbNameParam.IsNull)
        {
            var namedConn = _engine.GetConnectionByName(dbNameParam.GetVarChar());
            conn = namedConn ?? throw new Exception($"No database named {dbNameParam.GetVarChar()}");
        }
        else
        {
            var dbAndAsOf = dbParam.GetUInt64();
            asOf = TxId.From(PartitionId.Transactions.MakeEntityId(dbAndAsOf >> 16).Value);
            conn = _engine.GetConnectionByUid((ushort)(dbAndAsOf & 0xFFFF))!;
        }
        
        if (!asOfParam.IsNull)
            asOf = TxId.From(asOfParam.GetUInt64());

        asOf ??= TxId.MinValue;
        
        
        var attrId = conn.AttributeCache.GetAttributeId(_definition.PrimaryAttribute.Id);
        
        var bindData = new BindData
        {
            PrimaryAttributeId = attrId,
            Connection = conn,
            AsOf = asOf!.Value,
        };
        info.SetBindInfo(bindData);
    }
}
