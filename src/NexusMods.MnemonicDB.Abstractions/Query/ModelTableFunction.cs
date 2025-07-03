using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DuckDB.NET.Data;
using DuckDB.NET.Data.DataChunk.Writer;
using DuckDB.NET.Native;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

#pragma warning disable DuckDBNET001

namespace NexusMods.MnemonicDB.Abstractions.Query;

public class ModelTableFunction : IQueryFunction
{
    private readonly string _name;
    private readonly IAttribute _primaryAttribute;
    private readonly IAttribute[] _attributes;

    public ModelTableFunction(string name, IAttribute primaryAttribute, IAttribute[] attributes)
    {
        _name = name;
        _primaryAttribute = primaryAttribute;
        _attributes = attributes;

    }

    public void Register(DuckDBConnection connection, IQueryEngine engine)
    {
        connection.RegisterTableFunction(_name, () =>
        {
            List<ColumnInfo> columns = new();
            columns.Add(new ColumnInfo("Id", typeof(ulong)));
            foreach (var attribute in _attributes)
                columns.Add(new ColumnInfo(attribute.Id.Name, attribute.LowLevelType.ToClrType()));

            var db = engine.DefaultConnection().Db;
            return new TableFunction(columns, db.Datoms(_primaryAttribute)
                .Select(e => db.Get(e.E)));
        }, WriteRows);
    }

    private void WriteRows(object? o, IDuckDBDataWriter[] writers, ulong row)
    {
        var seg = (EntitySegment)o!;

        writers[0].WriteValue(seg.Id.Value, row);
        var cache = seg.Db.AttributeCache;
        for (int i = 1; i < writers.Length; i++)
        {
            var attr = _attributes.ElementAt(i - 1);
            var aId = cache.GetAttributeId(attr.Id);
            var offset = seg.FirstOffsetOf(aId);
            if (offset == -1)
            {
                if (attr.LowLevelType == ValueTag.Null)
                {
                    writers[i].WriteValue(false, row);
                    continue;
                }

                writers[i].WriteNull(row);
                continue;
            }

            var span = seg.GetValueSpan(offset, out var valueType);
            switch (valueType)
            {
                case ValueTag.Null:
                    writers[i].WriteValue(true, row);
                    break;
                case ValueTag.UInt8:
                    writers[i].WriteValue(UInt8Serializer.Read(span), row);
                    break;
                case ValueTag.UInt16:
                    writers[i].WriteValue(UInt16Serializer.Read(span), row);
                    break;
                case ValueTag.UInt32:
                    writers[i].WriteValue(UInt32Serializer.Read(span), row);
                    break;
                case ValueTag.UInt64:
                    writers[i].WriteValue(UInt64Serializer.Read(span), row);
                    break;
                case ValueTag.UInt128:
                    writers[i].WriteValue(UInt128Serializer.Read(span), row);
                    break;
                case ValueTag.Int16:
                    writers[i].WriteValue(Int16Serializer.Read(span), row);
                    break;
                case ValueTag.Int32:
                    writers[i].WriteValue(Int32Serializer.Read(span), row);
                    break;
                case ValueTag.Int64:
                    writers[i].WriteValue(Int64Serializer.Read(span), row);
                    break;
                case ValueTag.Int128:
                    writers[i].WriteValue(Int128Serializer.Read(span), row);
                    break;
                case ValueTag.Float32:
                    writers[i].WriteValue(Float32Serializer.Read(span), row);
                    break;
                case ValueTag.Float64:
                    writers[i].WriteValue(Float64Serializer.Read(span), row);
                    break;
                case ValueTag.Ascii:
                    writers[i].WriteValue(AsciiSerializer.Read(span), row);
                    break;
                case ValueTag.Utf8:
                    writers[i].WriteValue(Utf8Serializer.Read(span), row);
                    break;
                case ValueTag.Utf8Insensitive:
                    writers[i].WriteValue(Utf8InsensitiveSerializer.Read(span), row);
                    break;
                case ValueTag.Blob:
                    writers[i].WriteValue(Encoding.ASCII.GetString(span), row);
                    break;
                case ValueTag.HashedBlob:
                    writers[i].WriteValue(Encoding.ASCII.GetString(span), row);
                    break;
                case ValueTag.Reference:
                    writers[i].WriteValue(UInt64Serializer.Read(span), row);
                    break;
                case ValueTag.Tuple2_UShort_Utf8I:
                    var tpl = Tuple2_UShort_Utf8I_Serializer.Read(span);
                    writers[i].WriteValue(tpl.Item2, row);
                    break;
                case ValueTag.Tuple3_Ref_UShort_Utf8I:
                    var tp3 = Tuple3_Ref_UShort_Utf8I_Serializer.Read(span);
                    writers[i].WriteValue(tp3.Item3, row);
                    break;
                default:
                    throw new NotImplementedException($"No writer for {valueType}");
            }
        }
    }
}
