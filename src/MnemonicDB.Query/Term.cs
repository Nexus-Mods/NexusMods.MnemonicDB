namespace MnemonicDB.Query;

public static class Term
{
    public static Term<T> LVar<T>() => new(Query.LVar.New());
    public static Term<T> Constant<T>(T constant) => new(constant);

}

/// <summary>
/// A slot for a constant or a variable
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct Term<T>
{
    public Term(LVar lvar)
    {
        LVar = lvar;
        Constant = default!;
    }
    
    public Term(T constant)
    {
        LVar = default!;
        Constant = constant;
    }
    public LVar LVar { get; }
    public T Constant { get; }
    
    public bool Grounded => !LVar.Valid;
    public bool Ungrounded => LVar.Valid;
    
    public static implicit operator Term<T>(T constant) => new(constant);
    
}
