namespace NexusMods.MnemonicDB.QueryEngine;

public interface ILVar
{
    public IValueBox MakeBox();
}

public interface ILVarBox
{

}

public readonly struct LVar<T>(int id) : ILVar
{
    public override string ToString()
    {
        return $"LVar<{typeof(T).Name}>({id})";
    }

    public IValueBox MakeBox() => new ValueBox<T>();
    public int Id => id;
}
