namespace NexusMods.Query.Abstractions.Engines.Abstract;

public enum BindingType
{
    /// <summary>
    /// The binding is a constant, a value
    /// </summary>
    Constant,
    
    /// <summary>
    /// The binding is a variable that will be populated by the engine as it executes
    /// </summary>
    Variable,
    
    /// <summary>
    /// The binding is an output
    /// </summary>
    Output
}
