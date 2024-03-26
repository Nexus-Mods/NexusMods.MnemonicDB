using System.Text;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Helpers;

public static class ExtensionMethods
{
    public static IEnumerable<ObjectTuple> ToObjectDatoms(this IEnumerable<IReadDatom> datoms, IAttributeRegistry registry)
    {
        foreach (var datom in datoms)
        {
            var aSym = registry.GetSymbolForAttribute(datom.AttributeType);
            yield return new ObjectTuple
            {
                E = datom.E,
                A = aSym.Name,
                V = datom.ObjectValue,
                T = datom.T,
            };
        }
    }

    public static string ToTable(this IEnumerable<IReadDatom> datoms, IAttributeRegistry registry)
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

        var sb = new StringBuilder();
        foreach (var datom in datoms)
        {
            var isAssert = true;

            var symColumn = TruncateOrPad(registry.GetSymbolForAttribute(datom.AttributeType).Name, 24);
            var attrId = registry.GetAttributeId(datom.AttributeType).Value.ToString("X4");

            sb.Append(isAssert ? "+" : "-");
            sb.Append(" | ");
            sb.Append(datom.E.Value.ToString("X16"));
            sb.Append(" | ");
            sb.Append($"({attrId}) {symColumn}");
            sb.Append(" | ");

            switch (datom.ObjectValue)
            {
                case EntityId eid:
                    sb.Append(eid.Value.ToString("X16").PadRight(48));
                    break;
                case ulong ul:
                    sb.Append(ul.ToString("X16").PadRight(48));
                    break;
                default:
                    sb.Append(TruncateOrPad(datom.ObjectValue.ToString()!, 48));
                    break;
            }

            sb.Append(" | ");
            sb.Append(datom.T.Value.ToString("X16"));

            sb.AppendLine();

        }
        return sb.ToString();
    }
}
