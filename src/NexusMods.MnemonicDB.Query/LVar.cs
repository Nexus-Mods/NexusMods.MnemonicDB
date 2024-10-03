namespace NexusMods.MnemonicDB.Query;

public record struct LVar<TType>(string Name)
{
    public static LVar<TType> Bound => new(string.Intern("<bound>"));
    
    public static LVar<TType> Create(string name) => new(string.Intern(name));
}
