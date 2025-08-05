using System;

namespace NexusMods.HyperDuck;

public interface IBindingConverter
{
    public bool CanConvert(Type type, out int priority);

    public void Bind<T>(PreparedStatement stmt, int index, T value);
}

