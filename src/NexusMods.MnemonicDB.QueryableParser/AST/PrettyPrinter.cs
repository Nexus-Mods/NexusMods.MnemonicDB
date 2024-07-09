using System;
using System.Text;

namespace NexusMods.MnemonicDB.QueryableParser.AST;

internal struct Context
{
    public readonly StringBuilder Builder;
    public int Indentation;

    public Context()
    {
        Indentation = 0;
        Builder = new StringBuilder();
    }
    
    public string Result => Builder.ToString();

    public void Indent() => Indentation++;
    public void Unindent() => Indentation--;
    
    public void Line(string value)
    {
        for (var i = 0; i < Indentation; i++)
            Builder.Append("  ");
        Builder.AppendLine(value);
    }
}

public static class PrettyPrinter
{
    public static string Print(INode node)
    {
        var context = new Context();
        
        PrintNode(node, context);
        
        return context.Result;
    }

    private static void PrintNode(INode node, Context context)
    {
        switch (node)
        {
            case Conjunction conjunction:
                context.Line("Conjunction:");
                context.Indent();
                foreach (var child in conjunction.Nodes)
                {
                    PrintNode(child, context);
                }
                break;
            case PropertyAccess propertyAccess:
                context.Line($"{propertyAccess.Source}.{propertyAccess.Property.Name} -> {propertyAccess.Output}");
                break;
            case Root root:
                context.Line($"Root: {root.Method.Name} -> {root.To}");
                break;
            case Constant constant:
                context.Line($"{constant.Value} -> {constant.To}");
                break;
            case Unify unify:
                context.Line($"{unify.Left} == {unify.Right}");
                break;
            default:
                throw new NotSupportedException($"Node type '{node.GetType()}' is not supported.");
            
        }
    }
}
