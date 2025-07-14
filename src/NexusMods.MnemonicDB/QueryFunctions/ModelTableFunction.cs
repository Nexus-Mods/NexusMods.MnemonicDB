using System;
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

    public ModelTableFunction(IConnection conn, ModelDefinition definition, string prefix = "mdb")
    {
        _prefix = prefix;
        _conn = conn;
        _definition = definition;
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
        while (primaryIterator.MoveNext())
        {
            idVec[rowsEmitted] = primaryIterator.KeyPrefix.E.Value;
            rowsEmitted++;
        }
        
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
            case ValueTag.Utf8:
                WriteVarCharColumn(ids, vector, datoms);
                break;
            default:
                throw new NotImplementedException("Not implemented for " + tag);
        }
    }

    private void WriteVarCharColumn(ReadOnlySpan<EntityId> ids, WritableVector vector, ILightweightDatomSegment datoms)
    {
        for (var rowIdx = 0; rowIdx < ids.Length; rowIdx++)
        {
            var rowId = ids[rowIdx];
            if (datoms.FastForwardTo(rowId))
            {
                vector.WriteUtf8((ulong)rowIdx, datoms.ValueSpan);
            }
        }
    }

    private class Iterators
    {
        public required ILightweightDatomSegment PrimaryIterator;
        public required ILightweightDatomSegment[] InnerIterators;
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

    protected override void Bind(BindInfo info)
    {
        info.AddColumn<ulong>("Id");
        foreach (var attr in _definition.AllAttributes)
        {
            info.AddColumn(attr.Id.Name, attr.LowLevelType.DuckDbType());
        }
    }
}
