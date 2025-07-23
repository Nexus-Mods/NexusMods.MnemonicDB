namespace NexusMods.HyperDuck.Adaptor;

public interface IValueCursor
{
    /// <summary>
    /// Get the current value pointed at by the cursor
    /// </summary>
    public T GetValue<T>() where T : unmanaged;
    
    /// <summary>
    /// Get the vector of the list child
    /// </summary>
    public ReadOnlyVector GetListChild();
}
