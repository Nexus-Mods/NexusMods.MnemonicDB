using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.QueryFunctions;

public class DatomsTableFunction : ATableFunction
{
    private readonly IAttribute[] _attrs;
    private IQueryEngine? _engine;
    private readonly IConnection _conn;
    private readonly string _prefix;
    private readonly ushort[] _attrIdToEnum;
    private readonly Dictionary<string, IAttribute> _attrsByShortName;
    private readonly QueryEngine _queryEngine;

    private enum UnionOrdering : byte
    {
        Bool = 0,
        UInt64, // 64 bit unsigned integer
        String,
        Reference, // Reference to another MDB entity
        End = Reference,
    }

    public DatomsTableFunction(IConnection conn, IEnumerable<IAttribute> attrs, IQueryEngine queryEngine, string prefix = "mdb")
    {
        _prefix = prefix;
        _conn = conn;
        _engine = null;
        _attrs = attrs.OrderBy(attr => attr.Id.Id).ToArray();
        _attrsByShortName = attrs.ToDictionary(a => $"{a.Id.Namespace.Split(".").Last()}/{a.Id.Name}");
        var cache = conn.AttributeCache;
        _attrIdToEnum = new ushort[cache.MaxAttrId + 1];
        _queryEngine = (QueryEngine)queryEngine;
        for (int i = 0; i < attrs.Count(); i++)
        {
            var attr = _attrs[i];
            _attrIdToEnum[cache.GetAttributeId(attr.Id).Value] = (ushort)i;
        }
    }
    
    protected override void Setup(RegistrationInfo info)
    {
        info.SetName($"{_prefix}_Datoms");
        info.AddNamedParameter<string>("A");
        info.AddNamedParameter<bool>("History");
        info.SupportsPredicatePushdown();
    }

    protected override void Execute(FunctionInfo functionInfo)
    {
        var state = functionInfo.GetBindInfo<State>();
        if (state.Mode == ScanMode.AllAttributes)
            ExecuteAllAttributes(functionInfo, state);
        else
        {
            ExecuteOneAttribute(functionInfo, state);
        }
    }

    private void ExecuteOneAttribute(FunctionInfo functionInfo, State state)
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
        
        var iterator = state.Segment;
        var row = 0;
        while (iterator.MoveNext())
        {
            if (!eVec.IsEmpty)
                eVec[row] = iterator.KeyPrefix.E.Value;
            if (!tVec.IsEmpty) 
                tVec[row] = iterator.KeyPrefix.T.Value;
            if (state.History)
                isRetract[row] = iterator.KeyPrefix.IsRetract ? (byte)1 : (byte)0;
            
            if (vVec.IsValid)
            {
                switch (lowLevelType)
                {
                    case ValueTag.UInt64:
                        vData.CastFast<byte, ulong>()[row] = UInt64Serializer.Read(iterator.ValueSpan);
                        break;

                    case ValueTag.Utf8:
                        vVec.WriteUtf8((ulong)row, iterator.ValueSpan);
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

    private void ExecuteAllAttributes(FunctionInfo functionInfo, State state)
    {
        
        var chunk = functionInfo.Chunk;
        var eVec = chunk[0].GetData<ulong>();
        var aVec = chunk[1].GetData<byte>();
        //var vVec = chunk[2].GetData();
        var tVec = chunk[3].GetData<ulong>();
        var vVec = chunk[2];
        var vValidity = vVec.GetValidityMask();
        var vTag = vVec.GetStructChild(0).GetData<byte>();
        


        var strValidity = vVec.GetStructChild((ulong)UnionOrdering.String+1).GetValidityMask();
        var row = 0;

        for (int subCol = 0; subCol <= (int)UnionOrdering.End; subCol++)
            vVec.GetStructChild(0).GetValidityMask();
        
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
                case ValueTag.UInt64:
                    vTag[row] = (byte)UnionOrdering.UInt64;
                    var subVec = vVec.GetStructChild((ulong)(UnionOrdering.UInt64 + 1));
                    subVec.GetData<ulong>()[row] = UInt64Serializer.Read(iterator.ValueSpan);
                    var subVecMask = subVec.GetValidityMask();
                    subVecMask[(ulong)row] = true;
                    vValidity[(ulong)row] = true;
                    break;
                case ValueTag.Reference:
                    vTag[row] = (byte)UnionOrdering.Reference;
                    vVec.GetStructChild((ulong)(UnionOrdering.Reference + 1))
                        .GetData<ulong>()[row] = UInt64Serializer.Read(iterator.ValueSpan);
                    vValidity[(ulong)row] = true;
                    break;
                case ValueTag.Utf8 or ValueTag.Utf8Insensitive or ValueTag.Ascii:
                    vTag[row] = (byte)UnionOrdering.String;
                    var structMember = vVec.GetStructChild((ulong)(UnionOrdering.String + 1));
                    structMember.WriteUtf8((ulong)row, iterator.ValueSpan);;
                    var vMask = structMember.GetValidityMask();
                    vMask[(ulong)row] = true;
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
        var aParam = info.GetParameter("A");
        var historyParam = info.GetParameter("History");
        var history = !historyParam.IsNull && historyParam.GetBool();
        IDb db = _conn.Db;
        if (history)
        {
            db = _conn.History();
        }
        
        State state;
        if (!aParam.IsNull)
        {
            var attrToFind = aParam.GetVarChar();
            if (!_attrsByShortName.TryGetValue(attrToFind, out var attr))
                throw new Exception($"Attribute '{attrToFind}' not found");

            state = new State
            {
                Mode = ScanMode.SingleAttribute,
                Attribute = attr,
                Segment = db.LightweightDatoms(SliceDescriptor.Create(attr, _conn.AttributeCache))
            };
        }
        else
        {
            state = new State
            {
                Mode = ScanMode.AllAttributes,
                Attribute = null,
                Segment = db.LightweightDatoms(SliceDescriptor.AllEntities(PartitionId.Entity))
            };
        }
        
        info.SetBindInfo(state);
        if (state.Mode == ScanMode.SingleAttribute)
        {
            info.AddColumn<ulong>("E");
            info.AddColumn("V", state.Attribute!.LowLevelType.DuckDbType());
            info.AddColumn<ulong>("T");
        }
        else
        {
            info.AddColumn<ulong>("E");
            info.AddColumn("A", _queryEngine.AttrEnum);
            info.AddColumn("V", _queryEngine.ValueUnion);
            info.AddColumn<ulong>("T");
        }
        
        state.History = history;
        if (history)
            info.AddColumn<bool>("IsRetract");
    }

    private enum ScanMode
    {
        SingleAttribute,
        AllAttributes,
    }

    private class State
    {
        public bool History = false;
        public required ScanMode Mode;
        public required IAttribute? Attribute;
        public required ILightweightDatomSegment Segment;
    }
}
