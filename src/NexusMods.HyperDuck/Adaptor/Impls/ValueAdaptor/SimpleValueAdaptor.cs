using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;

public interface IConverter<TFrom, TTo> where TFrom : unmanaged
{
    public static abstract void Convert(TFrom from, ref TTo to);
}

public class SimpleValueAdaptor<TFrom, TTo, TConverter> : IValueAdaptor<TTo>
    where TFrom : unmanaged
    where TConverter : IConverter<TFrom, TTo>

{
    public static void Adapt<TCursor>(TCursor cursor, ref TTo? value) where TCursor : IValueCursor, allows ref struct
    {
        if (cursor.IsNull)
        {
            value = default;
            return;
        }

        var rawValue = cursor.GetValue<TFrom>();
        TConverter.Convert(rawValue, ref value!);
    }
}

public class SimpleValueAdaptorFactory<TFrom, TTo> : IValueAdaptorFactory 
    where TFrom : unmanaged
{
    private readonly DuckDbType _dataType;
    private readonly Func<TFrom, TTo> _converter;
    private readonly Type _converterType;
    private readonly Type _adaptorType;

    public SimpleValueAdaptorFactory(Func<TFrom, TTo> converter)
    {
        _converter = converter;
        _converterType = RuntimeConverterBuilder.CreateConverterType<TFrom, TTo>(converter);
        _adaptorType = typeof(SimpleValueAdaptor<,,>).MakeGenericType(typeof(TFrom), typeof(TTo), _converterType);
        
        _dataType = DuckDbTypeExtensions.ToDuckDbEnum<TFrom>();
    }
    public bool TryExtractType(DuckDbType taggedType, LogicalType logicalType, Type type, out Type[] subTypes, out int priority)
    {
        if (taggedType == _dataType && type == typeof(TTo))
        {
            priority = 1;
            subTypes = [];
            return true;
        }
        
        priority = 0;
        subTypes = [];
        return false;
    }

    public Type CreateType(Registry registry, DuckDbType taggedType, LogicalType logicalType, Type resultTypes, Type[] subTypes,
        Type[] subAdaptors)
    {
        return _adaptorType;
    }
}

file static class RuntimeConverterBuilder
{
    private static readonly AssemblyBuilder s_asm =
        AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("NexusMods.HyperDuck.RuntimeConverters"), AssemblyBuilderAccess.Run);

    private static readonly ModuleBuilder s_mod = s_asm.DefineDynamicModule("Main");

    public static Type CreateConverterType<TFrom, TTo>(Func<TFrom, TTo> converter)
        where TFrom : unmanaged
    {
        // Unique name per converter instance
        var name = $"FuncConverter_{typeof(TFrom).FullName}_{typeof(TTo).FullName}_{Guid.NewGuid():N}";
        var tb = s_mod.DefineType(name, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);

        // Implement IConverter<TFrom, TTo>
        var iface = typeof(IConverter<,>).MakeGenericType(typeof(TFrom), typeof(TTo));
        tb.AddInterfaceImplementation(iface);

        // public static Func<TFrom, TTo> Converter;
        var funcType = typeof(Func<,>).MakeGenericType(typeof(TFrom), typeof(TTo));
        var converterField = tb.DefineField("Converter", funcType, FieldAttributes.Public | FieldAttributes.Static);

        // public static void Convert(TFrom from, ref TTo to)
        var method = tb.DefineMethod(
            "Convert",
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
            CallingConventions.Standard,
            typeof(void),
            new[] { typeof(TFrom), typeof(TTo).MakeByRefType() });

        // Implement the interface method
        var ifaceMethod = iface.GetMethod("Convert", BindingFlags.Public | BindingFlags.Static)!;
        tb.DefineMethodOverride(method, ifaceMethod);

        var il = method.GetILGenerator();
        // to = Converter(from);
        // Load address of 'to' (arg1)
        il.Emit(OpCodes.Ldarg_1);
        // Load Converter field
        il.Emit(OpCodes.Ldsfld, converterField);
        // Load 'from' (arg0)
        il.Emit(OpCodes.Ldarg_0);
        // Call Invoke on Func<TFrom, TTo>
        var invoke = funcType.GetMethod("Invoke")!;
        il.Emit(OpCodes.Callvirt, invoke);
        // Store result into 'to'
        il.Emit(OpCodes.Stobj, typeof(TTo));
        il.Emit(OpCodes.Ret);

        var type = tb.CreateType()!;
        // Set the static field to the provided delegate
        type.GetField("Converter", BindingFlags.Public | BindingFlags.Static)!.SetValue(null, converter);
        return type;
    }
}
