namespace NexusMods.MnemonicDB.QueryableParser.AST;

public interface Constant : INode
{
    object Value { get; }
    LVar To { get; }
}

public record Constant<T>(T Value, LVar To) : Constant
{
    object Constant.Value => (object)Value!;
}
