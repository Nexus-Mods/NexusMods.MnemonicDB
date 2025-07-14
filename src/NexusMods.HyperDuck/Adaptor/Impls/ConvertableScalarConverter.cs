using System.Collections.Generic;
using System.Linq.Expressions;

namespace NexusMods.HyperDuck.Adaptor.Impls;

public class ConvertableScalarConverter<T>(DuckDbType typeId): IConverter
{
    public int? CanConvert(BuilderContext ctx)
    {
        return ctx.Mode == BuilderContext.ContextMode.Value && ctx.Types[0].TypeId == typeId  ? 0 : null;
    }

    public List<Expression> ConvertExpr(BuilderContext context)
    {
        return [Expression.Convert(context.CurrentValueExpr, typeof(T))];
    }
}
