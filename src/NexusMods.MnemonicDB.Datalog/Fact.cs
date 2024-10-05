using System.Collections.Generic;
using System.Text;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Datalog;

public  record Fact
{
    public Fact(Symbol predicate, params object[] args)
    {
        Predicate = predicate;
        Args = args;
    }

    public object[] Args { get; private set; }

    public Symbol Predicate { get; private set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(Predicate);
        sb.Append("(");
        sb.AppendJoin(", ", Args);
        sb.Append(")");
        return sb.ToString();
    }

    public bool Matches(Fact other)
    {
        if (!Equals(Predicate, other.Predicate))
            return false;

        if (Args.Length != other.Args.Length)
            return false;

        for (var i = 0; i < Args.Length; i++)
        {
            var arg1 = Args[i];
            var arg2 = other.Args[i];
            
            if (arg2 is LVar)
                continue;
                
            if (!Equals(arg1, arg2))
                return false;
        }
        
        return true;
    }
    
    public Fact Substitute(Dictionary<LVar, object> bindings)
    {
        var newArgs = new object[Args.Length];
        for (var i = 0; i < Args.Length; i++)
        {
            var arg = Args[i];
            if (arg is LVar lvar && bindings.TryGetValue(lvar, out var value))
            {
                newArgs[i] = value;
            }
            else
            {
                newArgs[i] = arg;
            }
        }
        return new Fact(Predicate, newArgs);
    }
}
