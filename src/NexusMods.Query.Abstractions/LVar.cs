namespace NexusMods.Query.Abstractions;

public class LVar<T>(string? Name = null) : ILVar
{ 
    public ILVarBox MakeBox() => new LVarBox<T>();
}
