namespace NexusMods.Query.Abstractions;

/// <summary>
/// Facts are the smallest unit of information in the database. They are most often tuple-like structures
/// </summary>
public interface IFact
{
    
}


/// <summary>
/// A typed fact, with one field
/// </summary>
public interface IFact<TA> : IFact
{
    
}

/// <summary>
/// A typed fact, with two fields
/// </summary>
public interface IFact<TA, TB> : IFact
{
    
}

/// <summary>
/// A typed fact, with three fields
/// </summary>
public interface IFact<TA, TB, TC> : IFact
{
    
}
