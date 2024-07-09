using System.Collections.Generic;
using NexusMods.MnemonicDB.QueryableParser.AST;

namespace NexusMods.MnemonicDB.QueryableParser;

public class ParseContext
{
    public List<INode> Nodes { get; } = new();
    public Stack<LVar> Variables { get; } = new();
    
    public LVar ReturnVar { get; set; } = LVar.New();
    public Dictionary<string, Stack<LVar>> Mappings { get; } = new();
    
    public void Push(LVar variable) => Variables.Push(variable);
    
    public LVar Pop() => Variables.Pop();
    
    public LVar Peek() => Variables.Peek();

    public LVar PushMapping(string name, LVar lvar)
    {
        if (!Mappings.TryGetValue(name, out var stack))
        {
            stack = new Stack<LVar>();
            Mappings.Add(name, stack);
        }
        stack.Push(lvar);
        return lvar;
    }
    
    public LVar PopMapping(string name) => Mappings[name].Pop();
    
    public bool TryGetMapping(string name, out LVar lvar)
    {
        lvar = default;
        return Mappings.TryGetValue(name, out var stack) 
               && stack.TryPeek(out lvar);
    }
    
    public void Add(INode node) => Nodes.Add(node);
}
