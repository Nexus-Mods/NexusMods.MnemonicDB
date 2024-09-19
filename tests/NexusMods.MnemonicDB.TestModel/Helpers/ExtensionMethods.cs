using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Text;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.TestModel.Helpers;

public static class ExtensionMethods
{
    public static string ToTable(this IEnumerable<Datom> datoms, AttributeCache cache)
    {
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
                    var val = MemoryMarshal.Read<EntityId>(datom.ValueSpan);
                    sb.Append(val.Value.ToString("X16").PadRight(48));
                    break;
                case ValueTags.Ascii:
                {
                    var size = MemoryMarshal.Read<uint>(datom.ValueSpan);
                    sb.Append(Encoding.ASCII.GetString(datom.ValueSpan.Slice(sizeof(uint), (int)size)));
                    break;
                }

                case ValueTags.UInt64:
                    var ul = MemoryMarshal.Read<ulong>(datom.ValueSpan);
                    sb.Append(ul.ToString("X16").PadRight(48));
                    break;
                case ValueTags.Blob:
                    var code = XxHash3.HashToUInt64(datom.ValueSpan);
                    var hash = code.ToString("X16");
                    sb.Append($"Blob 0x{hash} {datom.ValueSpan.Length} bytes".PadRight(48));
                    break;
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
