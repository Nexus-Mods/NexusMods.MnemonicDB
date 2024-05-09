using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GrEmit;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.ModelReflection.BaseClasses;

namespace NexusMods.MnemonicDB.ModelReflection;

public class Reflector(Type[] models)
{

    private Dictionary<Type, ModelDefinition> _modelDefinitions = new();

    public IEnumerable<ModelDefinition> Definitions => _modelDefinitions.Values.OrderBy(m => m.Name);

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

        if (model.GetMembers().FirstOrDefault(m => m.Name == "Attributes") is not Type attributeClass)
            throw new InvalidOperationException($"Model {model.FullName ?? model.Name} does not have an Attributes inner class");

        Dictionary<string, (IAttribute, FieldInfo)> attributes = new();

        foreach (var member in attributeClass.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (member.FieldType.IsAssignableTo(typeof(IAttribute)))
            {
                var value = member.GetValue(null) as IAttribute;
                if (value == null)
                    throw new InvalidOperationException($"Attribute {member.Name} is null");
                attributes.Add(member.Name, (value, member));

            }

        }

        List<ModelPropertyDefinition> modelMembers = new();

        foreach (var property in model.GetProperties())
        {
            if (property.GetCustomAttribute(typeof(From)) is not From attribute)
            {
                continue;
            }

            if (!attributes.TryGetValue(attribute.Name, out var dbAttr))
            {
                throw new InvalidOperationException($"Attribute {attribute.Name} not found in model {model.FullName ?? model.Name}");
            }

            modelMembers.Add(new ModelPropertyDefinition(property.Name, property, dbAttr.Item2, dbAttr.Item1));
        }

        var parentModels = model.GetInterfaces();
        var modelDefinition = new ModelDefinition(model, parentModels, attributes.Values.Select(m => m.Item1).ToArray(), modelMembers.ToArray());

        _modelDefinitions[model] = modelDefinition;

    }

    public void BuildAssembly()
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("NexusMods.MnemonicDB.Models." + Guid.NewGuid()),
            AssemblyBuilderAccess.Run);

        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

        foreach (var model in _modelDefinitions.Values)
        {
            var readonlyType = MakeReadonly(moduleBuilder, model);

        }

    }

    public TypeBuilder MakeReadonly(ModuleBuilder moduleBuilder, ModelDefinition modelDefinition)
    {
        var name = $"{modelDefinition.Name}_ReadOnly";
        var typeBuilder = moduleBuilder.DefineType(name, TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.NotPublic, typeof(ReadOnlyBase));
        typeBuilder.AddInterfaceImplementation(modelDefinition.Type);

        var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(IDb), typeof(EntityId) });

        var il = constructor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Calli, typeof(ReadOnlyBase).GetConstructor(new[] { typeof(IDb), typeof(EntityId) })!);
        il.Emit(OpCodes.Ret);

        foreach (var property in modelDefinition.Properties)
        {
            // Implement the interface properties
            var propertyBuilder = typeBuilder.DefineProperty(property.Name, PropertyAttributes.None, property.Property.PropertyType, null);
            var getMethod = typeBuilder.DefineMethod($"get_{property.Name}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, property.Property.PropertyType, []);
            getMethod.SetParameters([]);
            var p = getMethod.GetParameters();
            propertyBuilder.SetGetMethod(getMethod);
            using var getIl = new GroboIL(getMethod);

            getIl.Ldarg(0);
            getIl.Ldfld(property.AttributeField);
            var innerMethod = typeof(ReadOnlyBase).GetMethod("Get", BindingFlags.NonPublic | BindingFlags.Instance)!;
            innerMethod = innerMethod.MakeGenericMethod([typeof(string), typeof(string)]);
            getIl.Call(innerMethod);
            getIl.Ret();
        }

        var type = typeBuilder.CreateType();
        return typeBuilder;

    }

}
