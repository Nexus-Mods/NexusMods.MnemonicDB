using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Primitives;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.TestModel.Helpers;

public static class ExtensionMethods
{
    public static string ToTable(this IEnumerable<Datom> datoms, AttributeCache cache)
    {
        var valueTagId = cache.GetAttributeId(AttributeDefinition.ValueType.Id);
        var timestampId = cache.GetAttributeId(Transaction.Timestamp.Id);
        
        string TruncateOrPad(string val, int length)
        {
            if (val.Length > length)
            {
                var midPoint = length / 2;
                return (val[..(midPoint - 2)] + "..." + val[^(midPoint - 2)..]).PadRight(length);
            }

            return val.PadRight(length);
        }

        var dateTimeCount = 0;

        var sb = new StringBuilder();
        foreach (var datom in datoms)
        {
            var isRetract = datom.IsRetract;

            var aName = cache.GetSymbol(datom.A);
            var symColumn = TruncateOrPad(aName.Name, 24);
            var attrId = datom.A.Value.ToString("X4");

            sb.Append(isRetract ? "-" : "+");
            sb.Append(" | ");
            sb.Append(datom.E.Value.ToString("X16"));
            sb.Append(" | ");
            sb.Append($"({attrId}) {symColumn}");
            sb.Append(" | ");
            


            switch (datom.Prefix.ValueTag)
            {
                case ValueTags.Reference:
                {
                    var val = MemoryMarshal.Read<EntityId>(datom.ValueSpan);
                    sb.Append(val.Value.ToString("X16").PadRight(48));
                    break;
                }
                case ValueTags.Ascii:
                {
                    var size = MemoryMarshal.Read<uint>(datom.ValueSpan);
                    var str = Encoding.ASCII.GetString(datom.ValueSpan.Slice(sizeof(uint), (int)size));
                    sb.Append(TruncateOrPad(str, 48));
                    break;
                }

                case ValueTags.Int64 when datom.A == timestampId:
                    sb.Append($"DateTime : {dateTimeCount++}".PadRight(48));
                    break;
                case ValueTags.UInt64:
                    var ul = MemoryMarshal.Read<ulong>(datom.ValueSpan);
                    sb.Append(("0x" + ul.ToString("X16")).PadRight(48));
                    break;
                case ValueTags.Blob:
                {
                    var code = XxHash3.HashToUInt64(datom.ValueSpan);
                    var hash = code.ToString("X16");
                    sb.Append($"{datom.Prefix.ValueTag} 0x{hash} {datom.ValueSpan.Length} bytes".PadRight(48));
                    break;
                }
                case ValueTags.HashedBlob:
                {
                    var code = XxHash3.HashToUInt64(datom.ValueSpan[sizeof(ulong)..]);
                    var hash = code.ToString("X16");
                    sb.Append($"{datom.Prefix.ValueTag} 0x{hash} {datom.ValueSpan.Length} bytes".PadRight(48));
                    break;
                }
                case ValueTags.Null:
                    sb.Append(" ".PadRight(48));
                    break;
                case ValueTags.UInt8 when datom.A == valueTagId:
                    var tag = MemoryMarshal.Read<ValueTags>(datom.ValueSpan);
                    sb.Append($"{tag}".PadRight(48));
                    break;
                case ValueTags.UInt8:
                    sb.Append(MemoryMarshal.Read<byte>(datom.ValueSpan).ToString().PadRight(48));
                    break;
                case ValueTags.UInt16:
                    sb.Append(MemoryMarshal.Read<ushort>(datom.ValueSpan).ToString().PadRight(48));
                    break;
                case ValueTags.Utf8:
                {
                    var str = Encoding.UTF8.GetString(datom.ValueSpan[sizeof(int)..]);
                    sb.Append(TruncateOrPad(str, 48));
                    break;
                }
                case ValueTags.Utf8Insensitive:
                {
                    var str = Encoding.UTF8.GetString(datom.ValueSpan[sizeof(int)..]);
                    sb.Append(TruncateOrPad(str, 48));
                    break;
                }
                case ValueTags.UInt32:
                {
                    var val = MemoryMarshal.Read<uint>(datom.ValueSpan);
                    sb.Append(val.ToString().PadRight(48));
                    break;
                }
                case ValueTags.UInt128:
                {
                    var val = MemoryMarshal.Read<ulong>(datom.ValueSpan);
                    sb.Append(val.ToString("X16").PadRight(48));
                    break;
                }
                case ValueTags.Int16:
                {
                    var val = MemoryMarshal.Read<short>(datom.ValueSpan);
                    sb.Append(val.ToString().PadRight(48));
                    break;
                }
                case ValueTags.Int32:
                {
                    var val = MemoryMarshal.Read<int>(datom.ValueSpan);
                    sb.Append(val.ToString().PadRight(48));
                    break;
                }
                case ValueTags.Int64:
                {
                    var val = MemoryMarshal.Read<long>(datom.ValueSpan);
                    sb.Append(val.ToString().PadRight(48));
                    break;
                }
                case ValueTags.Int128:
                {
                    var val = MemoryMarshal.Read<long>(datom.ValueSpan);
                    sb.Append(val.ToString("X16").PadRight(48));
                    break;
                }
                case ValueTags.Float32:
                {
                    var val = MemoryMarshal.Read<float>(datom.ValueSpan);
                    sb.Append(val.ToString().PadRight(48));
                    break;
                }
                case ValueTags.Float64:
                {
                    var val = MemoryMarshal.Read<double>(datom.ValueSpan);
                    sb.Append(val.ToString().PadRight(48));
                    break;
                }
                default:
                    var otherCode = XxHash3.HashToUInt64(datom.ValueSpan);
                    var otherHash = otherCode.ToString("X16");
                    sb.Append($"Other 0x{otherHash} {datom.ValueSpan.Length} bytes".PadRight(48));
                    break;
            }

            sb.Append(" | ");
            sb.Append(datom.T.Value.ToString("X16"));

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
