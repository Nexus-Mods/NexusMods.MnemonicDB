namespace NexusMods.MnemonicDB.Queryable.AbstractSyntaxTree;

public interface ILVar
{
}

public struct LVar<T> : ILVar
{
    public string Name { get; set; }
}
