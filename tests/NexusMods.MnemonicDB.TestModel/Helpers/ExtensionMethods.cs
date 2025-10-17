using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Text;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Traits;

namespace NexusMods.MnemonicDB.TestModel.Helpers;

public static class ExtensionMethods
{
    public static string ToTable(this IEnumerable<ValueDatom> datoms, AttributeResolver resolver)
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
            var resolved = resolver.Resolve(datom);
            var isRetract = datom.IsRetract;

            var aName = cache.GetSymbol(datom.A);
            var symColumn = TruncateOrPad(aName.Name, 24);
            var attrId = datom.A.Value.ToString("X4");

            sb.Append(isRetract ? "-" : "+");
            sb.Append(" | ");
            sb.Append(datom.E);
            sb.Append(" | ");
            sb.Append($"({attrId}) {symColumn}");
            sb.Append(" | ");

            var o = resolved.ObjectValue;
            if (o is DateTimeOffset)
            {
                sb.Append(TruncateOrPad("DateTime : " + timestampCount, 48));
                timestampCount++;
            }
            else if (datom.Tag == ValueTag.Blob)
            {
                var memory = (Memory<byte>)datom.Value;
                var hash = ((Memory<byte>)datom.Value).xxHash3();
                sb.Append(TruncateOrPad($"Blob {hash} {memory.Length} bytes" , 48));
            }
            else if (datom.Tag == ValueTag.HashedBlob)
            {
                var memory = (Memory<byte>)datom.Value;
                var hash = ((Memory<byte>)datom.Value).xxHash3();
                sb.Append(TruncateOrPad($"HashedBlob {hash} {memory.Length} bytes", 48));
            }
            else
            {
                sb.Append(TruncateOrPad(resolved.ObjectValue.ToString() ?? "", 48));
            }

            sb.Append(" | ");
            sb.Append(datom.T);

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
