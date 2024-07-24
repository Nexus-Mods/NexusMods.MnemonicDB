using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Queryable.AbstractSyntaxTree;

namespace NexusMods.MnemonicDB.Queryable;

public class RuleBuilder
{
    public RuleBuilder(Symbol name, params IArgument[] arguments)
    {
        Name = name;
        Arguments = arguments;
    }

    public IArgument[] Arguments { get; set; }
    public Symbol Name { get; set; }

    public static RuleBuilder New<TA>(Symbol name, out LVar<TA> a1)
    {
        throw new NotImplementedException();
    }
    
    public static RuleBuilder Where(IPredicate predicate)
    {
        throw new NotImplementedException();
    }
    
}
