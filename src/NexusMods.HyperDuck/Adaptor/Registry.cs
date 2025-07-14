using System;
using System.Collections.Generic;
using System.Linq;

namespace NexusMods.HyperDuck.Adaptor;

public class Registry
{
    private readonly IConverter[] _converters;

    public Registry(IEnumerable<IConverter> converters)
    {
        _converters = converters.ToArray();
    }

    public IConverter GetConverter(BuilderContext ctx)
    {
        IConverter? found = null;
        var bestPriority = int.MinValue;
        foreach (var converter in _converters)
        {
            var priority = converter.CanConvert(ctx) ?? int.MinValue;
            if (priority <= bestPriority) 
                continue;
            found = converter;
            bestPriority = priority;
        }
        if (found == null)
            throw new InvalidOperationException($"No converter for {ctx.ClrType} in the context of {ctx.Mode}");
        return found;
    }

}