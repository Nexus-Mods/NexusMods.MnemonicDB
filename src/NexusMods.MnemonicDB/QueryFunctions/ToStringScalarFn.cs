using NexusMods.HyperDuck;
using NexusMods.HyperDuck.Adaptor.Impls;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.QueryFunctions;

public class ToStringScalarFn : AScalarFunction
{
    private readonly QueryEngine _engine;
    private readonly string _prefix;

    public ToStringScalarFn(QueryEngine engine, string prefix)
    {
        _engine = engine;
        _prefix = prefix;
    }
    
    public override void Setup()
    {
        SetName($"{_prefix}_ToString");
        AddParameter<byte[]>();
        AddParameter(_engine.ValueTagEnum);
        SetReturnType<string>();
    }

    public override void Execute(ReadOnlyChunk chunk, WritableVector vector)
    {
        var blobVec = chunk.GetVector(0).GetData<StringElement>();
        var tagVec = chunk.GetVector(1).GetData<byte>();

        for (int i = 0; i < (int)chunk.Size; i++)
        {
            var span = blobVec[i].GetSpan();
            var tag = (ValueTag)tagVec[i];
            string output = tag switch
            {
                ValueTag.Null => "null",
                ValueTag.UInt8 => UInt8Serializer.Read(span).ToString(),
                ValueTag.UInt16 => UInt16Serializer.Read(span).ToString(),
                ValueTag.UInt32 => UInt32Serializer.Read(span).ToString(),
                ValueTag.UInt64 => UInt64Serializer.Read(span).ToString(),
                ValueTag.UInt128 => UInt128Serializer.Read(span).ToString(),
                ValueTag.Int16 => Int16Serializer.Read(span).ToString(),
                ValueTag.Int32 => Int32Serializer.Read(span).ToString(),
                ValueTag.Int64 => Int64Serializer.Read(span).ToString(),
                ValueTag.Int128 => Int128Serializer.Read(span).ToString(),
                ValueTag.Float32 => Float32Serializer.Read(span).ToString(),
                ValueTag.Float64 => Float64Serializer.Read(span).ToString(),
                ValueTag.Ascii => AsciiSerializer.Read(span),
                ValueTag.Utf8 => Utf8Serializer.Read(span),
                ValueTag.Utf8Insensitive => Utf8InsensitiveSerializer.Read(span),
                ValueTag.Blob => BlobSerializer.Read(span).ToString(),
                ValueTag.HashedBlob => HashedBlobSerializer.Read(span).ToString(),
                ValueTag.Reference => EntityId.From(UInt64Serializer.Read(span)).ToString(),
                ValueTag.Tuple2_UShort_Utf8I => Tuple2_UShort_Utf8I_Serializer.Read(span).ToString(),
                ValueTag.Tuple3_Ref_UShort_Utf8I => Tuple3_Ref_UShort_Utf8I_Serializer.Read(span).ToString(),
                _ => "UNKNOWN_VALUE_TAG"
            };

            vector.WriteUtf16((ulong)i, output);
        }
    }
}
