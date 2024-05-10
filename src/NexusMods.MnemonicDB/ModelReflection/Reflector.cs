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

    private Dictionary<Type, Type> _readOnlyModels = new();

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
            var type = readonlyType.CreateType();
            _readOnlyModels[model.Type] = type;
            break;
        }
    }

    public TModel MakeReadonly<TModel>(IDb db, EntityId id)
    {
        var type = _readOnlyModels[typeof(TModel)];
        return (TModel)Activator.CreateInstance(type, db, id)!;
    }

    public TypeBuilder MakeReadonly(ModuleBuilder moduleBuilder, ModelDefinition modelDefinition)
    {
        var name = $"{modelDefinition.Name}_ReadOnly";
        var typeBuilder = moduleBuilder.DefineType(name,
            TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.NotPublic,
            typeof(ReadOnlyBase));
        typeBuilder.AddInterfaceImplementation(modelDefinition.Type);

        MakeConstructor(typeBuilder);

        foreach (var property in modelDefinition.Properties)
        {
            var propertyBuilder = typeBuilder.DefineProperty(property.Name, PropertyAttributes.None, property.Property.PropertyType, null);
            MakeGetter(typeBuilder, property, propertyBuilder);
            MakeSetter(typeBuilder, property, propertyBuilder);
        }
        return typeBuilder;

    }

    private static void MakeSetter(TypeBuilder typeBuilder, ModelPropertyDefinition property,
        PropertyBuilder propertyBuilder)
    {
        var setMethod = typeBuilder.DefineMethod($"set_{property.Name}",
            MethodAttributes.Public |
            MethodAttributes.SpecialName |
            MethodAttributes.HideBySig |
            MethodAttributes.Virtual, typeof(void), [property.Property.PropertyType]);
        setMethod.SetParameters([property.Property.PropertyType]);
        propertyBuilder.SetSetMethod(setMethod);
        var ilSetMethod = setMethod.GetILGenerator();
        ilSetMethod.Emit(OpCodes.Ret);
        typeBuilder.DefineMethodOverride(setMethod, property.Property.SetMethod!);
    }

    private static void MakeGetter(TypeBuilder typeBuilder, ModelPropertyDefinition property,
        PropertyBuilder propertyBuilder)
    {
        var getMethod = typeBuilder.DefineMethod($"get_{property.Name}",
            MethodAttributes.Public |
            MethodAttributes.SpecialName |
            MethodAttributes.HideBySig |
            MethodAttributes.Virtual, property.Property.PropertyType, []);
        getMethod.SetParameters([]);
        propertyBuilder.SetGetMethod(getMethod);
        var ilMethod = getMethod.GetILGenerator();
        ilMethod.Emit(OpCodes.Ldarg_0);
        ilMethod.Emit(OpCodes.Ldsfld, property.AttributeField);
        ilMethod.Emit(OpCodes.Call, typeof(ReadOnlyBase).GetMethod("Get", BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod([typeof(string), typeof(string)]));
        ilMethod.Emit(OpCodes.Ret);
        typeBuilder.DefineMethodOverride(getMethod, property.Property.GetMethod!);
    }

    private static void MakeConstructor(TypeBuilder typeBuilder)
    {
        var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(IDb), typeof(EntityId) });

        var il = constructor.GetILGenerator();
        var ctor = typeof(ReadOnlyBase).GetConstructor([typeof(IDb), typeof(EntityId)]);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Call, ctor!);
        il.Emit(OpCodes.Ret);
    }
}
