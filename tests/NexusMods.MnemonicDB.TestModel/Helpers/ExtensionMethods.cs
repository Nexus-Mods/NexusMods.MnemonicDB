using System.Text;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.TestModel.Helpers;

public static class ExtensionMethods
{
    public static string ToTable(this IEnumerable<Datom> datoms, AttributeResolver resolver)
    {
        var cache = resolver.AttributeCache;
        int timestampCount = 0;
        
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
            var (e, a, v, t, isRetract) = resolver.Resolve(datom);

            var aName = cache.GetSymbol(a);
            var symColumn = TruncateOrPad(aName.Name, 24);
            var attrId = a.Value.ToString("X4");

            sb.Append(isRetract ? "-" : "+");
            sb.Append(" | ");
            sb.Append(e);
            sb.Append(" | ");
            sb.Append($"({attrId}) {symColumn}");
            sb.Append(" | ");
            
            if (v is DateTimeOffset)
            {
                sb.Append(TruncateOrPad("DateTime : " + timestampCount, 48));
                timestampCount++;
            }
            else if (datom.Tag == ValueTag.Blob)
            {
                var memory = (Memory<byte>)v;
                var hash = ((Memory<byte>)v).xxHash3();
                sb.Append(TruncateOrPad($"Blob {hash} {memory.Length} bytes" , 48));
            }
            else if (datom.Tag == ValueTag.HashedBlob)
            {
                var memory = (Memory<byte>)v;
                var hash = ((Memory<byte>)v).xxHash3();
                sb.Append(TruncateOrPad($"HashedBlob {hash} {memory.Length} bytes", 48));
            }
            else
            {
                sb.Append(TruncateOrPad(v.ToString() ?? "", 48));
            }

            sb.Append(" | ");
            sb.Append(t);

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
