using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Defines a result set from a query
/// </summary>
public interface IResultSet
{
    /// <summary>
    /// Advance to the next row in the result set, returns false if there are no more rows, the first row is never
    /// pre-advanced. So an false return value means there are no rows in the result set.
    /// </summary>
    public bool Next();

    /// <summary>
    /// The entity id of the current row
    /// </summary>
    public ulong EntityId { get; }

    /// <summary>
    /// The attribute of the current row
    /// </summary>
    public ulong Attribute { get; }

    /// <summary>
    /// Gets the transaction id of the current row
    /// </summary>
    public ulong Tx { get; }

    /// <summary>
    /// The value type of the current row
    /// </summary>
    public ValueTypes ValueType { get; }

    /// <summary>
    /// Gets the value of the current row as an int64
    /// </summary>
    public long ValueInt64 { get; }

    /// <summary>
    /// Gets the value of the current row as an uint64
    /// </summary>
    public ulong ValueUInt64 { get; }

    /// <summary>
    /// Gets the value of the current row as a string
    /// </summary>
    public string ValueString { get; }

    /// <summary>
    /// Gets the value of the current row as a UInt128
    /// </summary>
    public UInt128 ValueUInt128 { get; }

    /// <summary>
    /// Gets the value of the current row as a double
    /// </summary>
    public double ValueDouble { get; }

    /// <summary>
    /// Gets the value of the current row as a float
    /// </summary>
    public float ValueFloat { get; }

    /// <summary>
    /// Should only use this for testing, gets the value of the current row and boxes it
    /// returning the value as an object
    /// </summary>
    public object Value { get; }
}
