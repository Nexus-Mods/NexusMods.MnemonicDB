using System.Collections.Generic;
using System.Linq.Expressions;

namespace NexusMods.HyperDuck.Adaptor;

public interface IConverter
{
    /// <summary>
    /// Returns true if this converter can be used to convert data given the current context, returns null if
    /// the converter is invalid, otherwise returns an int that specifiies the relative priority of the converter. 
    /// </summary>
    public int? CanConvert(BuilderContext context);
    
    /// <summary>
    /// Creates a converter expression
    /// </summary>
    public List<Expression> ConvertExpr(BuilderContext context);
}