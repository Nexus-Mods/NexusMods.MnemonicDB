namespace NexusMods.Query.Abstractions.Engines.Abstract;

public enum Cardinality
{
    /// <summary>
    /// Each input will generate one (or zero) outputs
    /// </summary>
    One,
    
    /// <summary>
    /// Each input will either filter the input out or keep it in, but will not generate new outputs
    /// </summary>
    Filter,
    
    /// <summary>
    /// Each input will generate zero or more outputs
    /// </summary>
    Many
}
