using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Query;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.QueryFunctions;

public class ModelTableFunction : ATableFunction
{
    private readonly ModelDefinition _definition;
    private readonly IConnection _conn;
    private readonly string _prefix;
    
    /// <summary>
    /// Each time this function runs to completion we store the number of rows we emitted, and feed that data to
    /// duckdb on the next evocation of the function. 
    /// </summary>
    private int _cardinality;

    public ModelTableFunction(IConnection conn, ModelDefinition definition, string prefix = "mdb")
    {
        _prefix = prefix;
        _conn = conn;
        _definition = definition;
        _cardinality = -1;
    }
    
    protected override void Setup(RegistrationInfo info)
    {
        info.SetName($"{_prefix}_{_definition.Name}");
        info.SupportsPredicatePushdown();
        info.AddNamedParameter<ulong>("AsOf");
    }

    protected override void Execute(FunctionInfo functionInfo)
    {
        var initData = functionInfo.GetInitInfo<Iterators>();
        var bindData = functionInfo.GetBindInfo<BindData>();
        var outChunk = functionInfo.Chunk;
        if (!bindData.NextChunk(initData, out var idsChunk, out var emitSize))
        {
            outChunk.Size = 0;
            return;
        }

        outChunk.Size = (ulong)emitSize;
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
            var attrId = bindData.Db.AttributeCache.GetAttributeId(attr.Id);
            var iterator = bindData.Db.LightweightDatoms(SliceDescriptor.AttributesStartingAt(attrId, idVec[0]));
            var valueVector = outChunk[(ulong)columnIdx];
            WriteColumn(attr.LowLevelType, idVec, valueVector, iterator);
        }
        ArrayPool<EntityId>.Shared.Return(idsChunk);
    }

    private void WriteColumn(ValueTag tag, ReadOnlySpan<EntityId> ids, WritableVector vector, ILightweightDatomSegment datoms)
    {
        switch (tag)
        {
            case ValueTag.Null:
                WriteNullColumn(ids, vector, datoms);
                break;
            case ValueTag.UInt8:
                WriteUnmanagedColumn<byte>(ids, vector, datoms);
                break;
            case ValueTag.UInt16:
                WriteUnmanagedColumn<ushort>(ids, vector, datoms);
                break;
            case ValueTag.UInt32:
                WriteUnmanagedColumn<uint>(ids, vector, datoms);
                break;
            case ValueTag.UInt64:
                WriteUnmanagedColumn<ulong>(ids, vector, datoms);
                break;
            case ValueTag.UInt128:
                WriteUnmanagedColumn<UInt128>(ids, vector, datoms);
                break;
            case ValueTag.Int16:
                WriteUnmanagedColumn<short>(ids, vector, datoms);
                break;
            case ValueTag.Int32:
                WriteUnmanagedColumn<int>(ids, vector, datoms);
                break;
            case ValueTag.Int64:
                WriteUnmanagedColumn<long>(ids, vector, datoms);
                break;
            case ValueTag.Int128:
                WriteUnmanagedColumn<Int128>(ids, vector, datoms);
                break;
            case ValueTag.Float32:
                WriteUnmanagedColumn<float>(ids, vector, datoms);
                break;
            case ValueTag.Float64:
                WriteUnmanagedColumn<double>(ids, vector, datoms);
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
                WriteUnmanagedColumn<ulong>(ids, vector, datoms);
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

    private void WriteNullColumn(ReadOnlySpan<EntityId> ids, WritableVector vector, ILightweightDatomSegment datoms)
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

    private void WriteTuple3Column(ReadOnlySpan<EntityId> ids, WritableVector vector, ILightweightDatomSegment datoms)
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

    private void WriteTuple2Column(ReadOnlySpan<EntityId> ids, WritableVector vector, ILightweightDatomSegment datoms)
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

    private void WriteUnmanagedColumn<T>(ReadOnlySpan<EntityId> ids, WritableVector vector, ILightweightDatomSegment datoms) 
        where T : unmanaged
    {
        var dataSpan = vector.GetData<T>();
        for (var rowIdx = 0; rowIdx < ids.Length; rowIdx++)
        {
            var rowId = ids[rowIdx];
            if (datoms.FastForwardTo(rowId))
            {
                dataSpan[rowIdx] = datoms.ValueSpan.CastFast<byte, T>()[0];
            }
        }
    }

    private void WriteVarCharColumn(ReadOnlySpan<EntityId> ids, WritableVector vector, ILightweightDatomSegment datoms)
    {
        WritableValidityMask mask = default;
        for (var rowIdx = 0; rowIdx < ids.Length; rowIdx++)
        {
            var rowId = ids[rowIdx];
            if (datoms.FastForwardTo(rowId))
            {
                vector.WriteUtf8((ulong)rowIdx, datoms.ValueSpan);
            }
            else
            {
                if (!mask.IsValid)
                {
                    mask = vector.GetValidityMask();
                    mask.SetAllValid();
                }

                mask[(ulong)rowIdx] = false;
            }
        }
    }

    private class Iterators
    {
        public int NextId = 0;
    }

    protected override object? Init(InitInfo initInfo, InitData initData)
    {
        var bindData = initInfo.GetBindData<BindData>();
        initInfo.SetMaxThreads(bindData.Ids.Count);
        return new Iterators
        {
            NextId = 0,
        };
    }

    private class BindData
    {
        public required IDb Db;
        public required List<EntityId[]> Ids;
        public required int RowCount;
        public required int ChunkSize;

        public bool NextChunk(Iterators initData, out EntityId[] ids, out int emitSize)
        {
            var thisId = Interlocked.Increment(ref initData.NextId) - 1;
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
    protected override void Bind(BindInfo info)
    {
        info.AddColumn<ulong>("Id");
        foreach (var attr in _definition.AllAttributes)
        {
            info.AddColumn(attr.Id.Name, attr.LowLevelType.DuckDbType());
        }

        using var asOf = info.GetParameter("AsOf");
        IDb db = _conn.Db;
        if (!asOf.IsNull)
            db = db.Connection.AsOf(TxId.From(asOf.GetUInt64()));
            
        var attrId = db.AttributeCache.GetAttributeId(_definition.PrimaryAttribute.Id);
        
        // Load all the Ids here so we can load the rows in parallel.
        var totalRows = db.IdsForPrimaryAttribute(attrId, (int)GlobalConstants.DefaultVectorSize, out var chunks);
        info.SetCardinality((ulong)totalRows, true);
        
        var bindData = new BindData
        {
            Db = db,
            Ids = chunks,
            RowCount = totalRows,
            ChunkSize = (int)GlobalConstants.DefaultVectorSize,
        };
        info.SetBindInfo(bindData);
    }
}
