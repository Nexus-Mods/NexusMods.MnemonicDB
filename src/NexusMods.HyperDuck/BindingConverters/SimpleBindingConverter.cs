using System;

namespace NexusMods.HyperDuck.BindingConverters;

/// <summary>
/// A simple binding converter that uses a converter function to convert from one type to another.
/// </summary>
public class SimpleBindingConverter<TFrom, TTo>(Func<TFrom, TTo> converter) : IBindingConverter
{
    public bool CanConvert(Type type, out int priority)
    {
        if (type == typeof(TFrom))
        {
            priority = 10;
            return true;
        }
        else if (type.IsAssignableTo(typeof(TFrom)))
        {
            priority = 5;
            return true;
        }

        priority = -1;
        return false;
    }

    public void Bind<T>(PreparedStatement stmt, int index, T value)
    {
        var converted = converter((TFrom)(object?)value!);
        stmt.BindNative(index, converted);       
    }
}
