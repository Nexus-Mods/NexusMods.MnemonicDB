using System.Linq.Expressions;

namespace NexusMods.HyperDuck.Adaptor;

public interface IChunkConverter
{
    public int? CanConvert(BuilderContext to);
    public Expression ConvertExpr(BuilderContext to);
}