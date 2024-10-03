namespace NexusMods.MnemonicDB.Query;

public readonly record struct Term<TType>(LVar<TType> LVar, TType Value) 
{
    public bool IsLVar => LVar != LVar<TType>.Bound;
    
    public bool IsBound => !IsLVar;
    
    public static implicit operator Term<TType>(LVar<TType> term) => new(term, default!);
    public static implicit operator Term<TType>(TType term) => new(LVar<TType>.Bound, term);
}
