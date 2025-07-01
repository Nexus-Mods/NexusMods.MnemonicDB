using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.MnemonicDB.QueryV2;

public class ModelTableFunction : TableFunction
{
    private readonly ModelTableDefinition _definition;
    private readonly QueryEngine _engine;

    public ModelTableFunction(ModelTableDefinition definition, QueryEngine engine) : base("mdb_" + definition.Name, [])
    {
        _definition = definition;
        _engine = engine;
    }

    protected override void Write(DuckDBChunkWriter writer, object? state, object? initData)
    {
        var iteratorSet = (IteratorSet)initData!;
        
        Span<VectorValueWriter> columnWriters = stackalloc VectorValueWriter[iteratorSet.ColumnIterators.Length];
        for (var i = 0; i < iteratorSet.ColumnIterators.Length; i++)
        {
            if (i == iteratorSet.EIdIndex)
            {
                columnWriters[i] = writer.GetWriter(i, ValueTag.Reference);
                continue;
            }
            columnWriters[i] = writer.GetWriter(i, _definition.Attributes[i].LowLevelType);
        }
        
        var primaryIterator = iteratorSet.PrimaryIterator;
        int row = 0;
        while (true)
        {
            if (!primaryIterator.MoveNext())
                break;

            for (int column = 0; column < iteratorSet.ColumnIterators.Length; column++)
            {
                if (column == iteratorSet.EIdIndex)
                {
                    columnWriters[column].WriteValue(row, primaryIterator.KeyPrefix.E);
                    continue;
                }
                var iterator = iteratorSet.ColumnIterators[column];
                if (!iterator.FastForwardTo(primaryIterator.KeyPrefix.E))
                    continue;

                columnWriters[column].Write(row, iterator.ValueSpan);
            }

            row++;
        }

        writer.Length = (ulong)row;
    }

    private class IteratorSet
    {
        public required ILightweightDatomSegment PrimaryIterator;
        public required ILightweightDatomSegment[] ColumnIterators;
        public int EIdIndex = -1;
    }
    protected override object? Init(int[] columnMappings)
    {
        var conn = _engine.DefaultConnection();
        var db = conn.Db;
        var attrCache = db.AttributeCache;

        
        var columnCount = columnMappings.Sum(static x => x != -1 ? 1 : 0);
        var columns = new ILightweightDatomSegment[columnCount];
        
        var set = new IteratorSet
        {
            PrimaryIterator = db.LightweightDatoms(SliceDescriptor.Create(attrCache.GetAttributeId(_definition.PrimaryAttribute.Id))),
            ColumnIterators = columns
        };
        for (var inputId = 0; inputId < columnMappings.Length; inputId++)
        {
            var outputId = columnMappings[inputId];
            if (outputId == -1)
                continue;
            if (inputId == 0)
            {
                set.EIdIndex = outputId;
                continue;
            }
            
            var attr = _definition.Attributes[inputId - 1];
            var attrId = attrCache.GetAttributeId(attr.Id);
            columns[outputId] = db.LightweightDatoms(SliceDescriptor.Create(attrId));
        }

        return set;
    }

    protected override void Bind(ref BindInfoWriter bind)
    {
        bind.AddColumn<ulong>("Id");
        foreach (var attr in _definition.Attributes)
        {
            bind.AddColumn(attr.Id.Name, attr.LowLevelType.ToLowLevelType());
        }
    }
}
