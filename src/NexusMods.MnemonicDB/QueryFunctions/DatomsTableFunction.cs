using System.Collections.Generic;
using System.Linq;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.QueryFunctions;

public class DatomsTableFunction : ATableFunction
{
    private readonly IAttribute[] _attrs;
    private readonly LogicalType _logicalType;
    private IQueryEngine? _engine;
    private readonly LogicalType _valueType;
    private readonly IConnection _conn;
    private readonly string _prefix;
    private readonly ushort[] _attrIdToEnum;

    private enum UnionOrdering : byte
    {
        Bool = 0,
        Reference, // Reference to another MDB entity
        String,

    }

    public DatomsTableFunction(IConnection conn, IEnumerable<IAttribute> attrs, string prefix = "mdb")
    {
        _prefix = prefix;
        _conn = conn;
        _engine = null;
        _attrs = attrs.OrderBy(attr => attr.Id.Id).ToArray();
        var cache = conn.AttributeCache;
        _attrIdToEnum = new ushort[cache.MaxAttrId + 1];
        for (int i = 0; i < attrs.Count(); i++)
        {
            var attr = _attrs[i];
            _attrIdToEnum[cache.GetAttributeId(attr.Id).Value] = (ushort)i;
        }

        _logicalType = LogicalType.CreateEnum(_attrs.Select(s => 
            $"{s.Id.Namespace.Split(".").Last()}/{s.Id.Name}").ToArray());

        _valueType = CreateValueType();
    }

    private LogicalType CreateValueType()
    {
        var boolType = LogicalType.From<bool>();
        var uInt8Type = LogicalType.From<byte>();
        var uInt16Type = LogicalType.From<ushort>();
        var uInt32Type = LogicalType.From<uint>();
        var uInt64Type = LogicalType.From<ulong>();
        var int16Type = LogicalType.From<short>();
        var int32Type = LogicalType.From<int>();
        var int64Type = LogicalType.From<long>();
        var stringType = LogicalType.From<string>();
        return LogicalType.CreateUnion([
            "bool",
            //"uint16",
            //"uint32",
            //"uint64",
            //"int16",
            //"int32",
            //"int64",
            "string",
            "ref"
        ],[
            boolType,
        //uInt8Type,
        //uInt16Type,
        //uInt32Type,
        //uInt64Type,
        //int16Type,
        //int32Type,
        //int64Type,
        //stringType
            uInt64Type,
            stringType,
        ]);
    }

    protected override void Setup(RegistrationInfo info)
    {
        info.SetName($"{_prefix}_Datoms");
    }

    protected override void Execute(FunctionInfo functionInfo)
    {
        
        var chunk = functionInfo.Chunk;
        var eVec = chunk[0].GetData<ulong>();
        var aVec = chunk[1].GetData<byte>();
        //var vVec = chunk[2].GetData();
        var tVec = chunk[2].GetData<ulong>();
        var vVec = chunk[3];
        var vValidity = vVec.GetValidityMask();
        var vTag = vVec.GetStructChild(0).GetData<byte>();

        var strValidity = vVec.GetStructChild((ulong)UnionOrdering.String+1).GetValidityMask();
        var state = functionInfo.GetBindInfo<State>();
        var row = 0;
        var iterator = state.Segment;
        while (iterator.MoveNext())
        {
            eVec[row] = iterator.KeyPrefix.E.Value;
            aVec[row] = (byte)_attrIdToEnum[iterator.KeyPrefix.A.Value];
            tVec[row] = iterator.KeyPrefix.T.Value;

            strValidity[(ulong)row] = false;
            switch (iterator.KeyPrefix.ValueTag)
            {
                case ValueTag.Null:
                    vTag[row] = (byte)UnionOrdering.Bool;
                    vVec.GetStructChild((ulong)(UnionOrdering.Bool + 1)).GetData<byte>()[row] = 1;
                    vValidity[(ulong)row] = true;
                    break;
                
                case ValueTag.Reference:
                    vTag[row] = (byte)UnionOrdering.Reference;
                    vVec.GetStructChild((ulong)(UnionOrdering.Reference + 1))
                        .GetData<ulong>()[row] = UInt64Serializer.Read(iterator.ValueSpan);
                    vValidity[(ulong)row] = true;
                    break;
                /*
                case ValueTag.Utf8Insensitive:
                    vTag[row] = (byte)UnionOrdering.String;
                    vVec.GetStructChild((ulong)UnionOrdering.String + 1)
                        .WriteUtf8((ulong)row, iterator.ValueSpan);
                    vValidity[(ulong)row] = true;
                    break;
                    */
                    
                
                default:
                    vValidity[(ulong)row] = false;
                    break;
            }

            row++;
            if (row >= eVec.Length)
                break;
        }
        chunk.Size = (ulong)row;
    }

    protected override void Bind(BindInfo info)
    {
        info.AddColumn<ulong>("E");
        info.AddColumn("A", _logicalType);
        info.AddColumn<ulong>("T");
        info.AddColumn("V", _valueType);
        //info.AddColumn<ulong>("V");
        info.SetBindInfo(new State
        {
            Segment = 
                _conn.Db.LightweightDatoms(SliceDescriptor.AllEntities(PartitionId.Entity))
        });
    }

    private class State
    {
        public required ILightweightDatomSegment Segment;
    }
}
