using System.Linq.Expressions;

namespace NexusMods.HyperDuck.Adaptor;

public interface IRowConverter
{
    public int? CanConvert(BuilderContext context);
    
    public Expression ConvertExpr(BuilderContext context);
}