using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NexusMods.MnemonicDB.Queryable.AbstractSyntaxTree;

public interface ILVar
{
    public string Name { get; }
    
    public ILVarBox MakeBox();
}

public interface ILVarBox
{
}

public struct LVar<T> : ILVar
{
    public string Name { get; set; }

    public override string ToString()
    {
        return $"?{Name}";
    }

    public ILVarBox MakeBox()
    {
        return new LVarBox<T>();
    }
}

public struct LVarBox<T> : ILVarBox
{
    public T Value;
}
