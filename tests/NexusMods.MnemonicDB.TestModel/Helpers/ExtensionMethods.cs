using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Text;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Traits;

namespace NexusMods.MnemonicDB.TestModel.Helpers;

public static class ExtensionMethods
{
    public static string ToTable(this IEnumerable<IDatomLikeRO> datoms, AttributeCache cache)
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
            sb.Append(datom.E);
            sb.Append(" | ");
            sb.Append($"({attrId}) {symColumn}");
            sb.Append(" | ");

            sb.Append(TruncateOrPad(datom.Value.ToString() ?? "", 48));

            sb.Append(" | ");
            sb.Append(datom.T);

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
