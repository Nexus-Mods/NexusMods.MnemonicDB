using System;
using System.Collections.Generic;
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
    }

    protected override void Execute(FunctionInfo functionInfo)
    {
        var iterators = functionInfo.GetInitInfo<Iterators>();
        var chunk = functionInfo.Chunk;
        
        var idVec = functionInfo.GetWritableVector(0).GetData<ulong>();
        var actualIdSpan = idVec;
        if (idVec.IsEmpty)
            idVec = new Span<ulong>(new ulong[functionInfo.EmitSize]);

        var rowsEmitted = 0;

        var primaryIterator = iterators.PrimaryIterator;
        while (primaryIterator.MoveNext() && rowsEmitted < idVec.Length)
        {
            idVec[rowsEmitted] = primaryIterator.KeyPrefix.E.Value;
            rowsEmitted++;
        }
        iterators.TotalRowsEmitted += rowsEmitted;

        // We reached the end of the primary iterator, so we can store the cardinality
        if (rowsEmitted < idVec.Length) 
            _cardinality = iterators.TotalRowsEmitted;
        
        // Slice the idVec so we can constrain the joining columns
        var ids = idVec.SliceFast(0, rowsEmitted).CastFast<ulong, EntityId>();
        functionInfo.SetEmittedRowCount(rowsEmitted);

        // Now write the other columns
        for (var columnIdx = 0; columnIdx < iterators.InnerIterators.Length; columnIdx++)
        {
            var iterator = iterators.InnerIterators[columnIdx];
            var fnIdx = functionInfo.EngineToFn(columnIdx);
            // No reason to copy the ids if they're already emitted
            if (fnIdx == 0)
                continue;

            var valueVector = chunk[(ulong)columnIdx];
            var attr = _definition.AllAttributes[fnIdx - 1];
            WriteColumn(attr.LowLevelType, ids, valueVector, iterator);
        }
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
        public required ILightweightDatomSegment PrimaryIterator;
        public required ILightweightDatomSegment[] InnerIterators;
        public int TotalRowsEmitted = 0;
    }

    protected override object? Init(InitData initData)
    {
        var attrCache = _conn.AttributeCache;
        var db = _conn.Db;
        var iterators = new ILightweightDatomSegment[initData.EngineToFn.Length];
        var primaryIterator = db.LightweightDatoms(SliceDescriptor.Create(_definition.PrimaryAttribute, attrCache));
        for (var i = 0; i < initData.EngineToFn.Length; i++)
        {
            var innerIdx = initData.EngineToFn[i];
            if (innerIdx == 0)
            {
                iterators[i] = db.LightweightDatoms(SliceDescriptor.Create(_definition.PrimaryAttribute, attrCache));
            }
            else
            {
                var innerAttr = _definition.AllAttributes[innerIdx - 1];
                iterators[i] = db.LightweightDatoms(SliceDescriptor.Create(innerAttr, attrCache));
            }
        }
        return new Iterators
        {
            PrimaryIterator = primaryIterator,
            InnerIterators = iterators,
        };
    }

    private class BindData
    {
        public IDb Db;
        public List<EntityId[]> Ids;
    }
    protected override void Bind(BindInfo info)
    {
        info.AddColumn<ulong>("Id");
        foreach (var attr in _definition.AllAttributes)
        {
            info.AddColumn(attr.Id.Name, attr.LowLevelType.DuckDbType());
        }
        
        var ids = 
        
        // If we have a memoized cardinality value, hand that off to DuckDB
        if (_cardinality != -1) 
            info.SetCardinality((ulong)_cardinality, false);
    }
}
