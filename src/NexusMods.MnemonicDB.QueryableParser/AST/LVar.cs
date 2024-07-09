using System.Threading;

namespace NexusMods.MnemonicDB.QueryableParser.AST;

public struct LVar
{
    private static int _counter = 1;
    
    private LVar(int id) => Id = id;
    public int Id { get; }

    public static LVar New() => new(Interlocked.Increment(ref _counter));

    public override string ToString()
    {
        return $"?{Id}";
    }
}
