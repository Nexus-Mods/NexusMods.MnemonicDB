using System;
using System.Reflection;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.ModelReflection;

public class Reflector(Type[] models)
{


    public void Process()
    {
        foreach (var type in models)
            Process(type);
    }

    private void Process(Type model)
    {
        var modelAttribute = model.GetCustomAttribute(typeof(ModelAttribute)) as ModelAttribute;
        if (modelAttribute != null)
            throw new InvalidOperationException($"Model {model.FullName ?? model.Name} does not have a ModelAttribute");

        foreach (var property in model.GetProperties())
        {
            if (property.GetCustomAttribute(typeof(DefineAttribute)) is not DefineAttribute attribute)
            {
                continue;
            }

            var memberName = attribute.Name ?? property.Name;
            var highLevelType = property.PropertyType;
            var lowLevelType = ResolveLowLevelType(highLevelType, attribute);
        }


    }

    private Type ResolveLowLevelType(Type highLevel, DefineAttribute defineAttribute)
    {
        return highLevel;
    }

}
