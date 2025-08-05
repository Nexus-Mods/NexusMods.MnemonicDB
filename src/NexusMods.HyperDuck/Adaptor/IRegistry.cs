namespace NexusMods.HyperDuck.Adaptor;

public interface IRegistry
{
    public IResultAdaptor<T> GetAdaptor<T>(Result result);

    IBindingConverter GetBindingConverter<T>(T obj);
}
