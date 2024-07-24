using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Queryable.AbstractSyntaxTree;

public interface IPredicate
{
}

public class Predicate<TArg1>(Symbol name, Argument<TArg1> a1) : IPredicate 
    where TArg1 : notnull
{
    
}

