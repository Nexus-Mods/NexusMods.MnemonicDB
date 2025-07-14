﻿using System.Collections.Generic;
using System.Linq.Expressions;

namespace NexusMods.HyperDuck.Adaptor.Impls;

public class StringValueConverter : IConverter
{
    public int? CanConvert(BuilderContext ctx)
    {
        return ctx.Mode == BuilderContext.ContextMode.Value && ctx.Types[0].TypeId == DuckDbType.Varchar  ? 0 : null;
    }

    public List<Expression> ConvertExpr(BuilderContext context)
    {
        return [Expression.Call(context.CurrentValueExpr, "GetString", null)];
    }
}
