using System;
using System.Runtime.CompilerServices;

namespace NexusMods.HyperDuck.Adaptor.Impls.RowAdaptors;

public class TupleAdaptor<T1, T2, TAdaptor1, TAdaptor2> : IRowAdaptor<(T1, T2)>
    where TAdaptor1 : IValueAdaptor<T1>
    where TAdaptor2 : IValueAdaptor<T2>
{
    public static void Adapt(RowCursor cursor, ref (T1, T2) value)
    {
        var valCursor = new ValueCursor(cursor);
        TAdaptor1.Adapt(valCursor, ref value.Item1!);
        valCursor.ColumnIndex++;
        TAdaptor2.Adapt(valCursor, ref value.Item2!);
    }
}

public class TupleAdaptor<T1, T2, T3, TAdaptor1, TAdaptor2, TAdaptor3> : IRowAdaptor<(T1, T2, T3)>
    where TAdaptor1 : IValueAdaptor<T1>
    where TAdaptor2 : IValueAdaptor<T2>
    where TAdaptor3 : IValueAdaptor<T3>
{
    public static void Adapt(RowCursor cursor, ref (T1, T2, T3) value)
    {
        var valCursor = new ValueCursor(cursor);
        TAdaptor1.Adapt(valCursor, ref value.Item1!);
        valCursor.ColumnIndex++;
        TAdaptor2.Adapt(valCursor, ref value.Item2!);
        valCursor.ColumnIndex++;
        TAdaptor3.Adapt(valCursor, ref value.Item3!);
    }
}


public class TupleAdaptor<T1, T2, T3, T4, TAdaptor1, TAdaptor2, TAdaptor3, TAdaptor4> : IRowAdaptor<(T1, T2, T3, T4)>
    where TAdaptor1 : IValueAdaptor<T1>
    where TAdaptor2 : IValueAdaptor<T2>
    where TAdaptor3 : IValueAdaptor<T3>
    where TAdaptor4 : IValueAdaptor<T4>
{
    public static void Adapt(RowCursor cursor, ref (T1, T2, T3, T4) value)
    {
        var valCursor = new ValueCursor(cursor);
        TAdaptor1.Adapt(valCursor, ref value.Item1!);
        valCursor.ColumnIndex++;
        TAdaptor2.Adapt(valCursor, ref value.Item2!);
        valCursor.ColumnIndex++;
        TAdaptor3.Adapt(valCursor, ref value.Item3!);
        valCursor.ColumnIndex++;
        TAdaptor4.Adapt(valCursor, ref value.Item4!);
    }
}


public class TupleAdaptor<T1, T2, T3, T4, T5, TAdaptor1, TAdaptor2, TAdaptor3, TAdaptor4, TAdaptor5> : IRowAdaptor<(T1, T2, T3, T4, T5)>
    where TAdaptor1 : IValueAdaptor<T1>
    where TAdaptor2 : IValueAdaptor<T2>
    where TAdaptor3 : IValueAdaptor<T3>
    where TAdaptor4 : IValueAdaptor<T4>
    where TAdaptor5 : IValueAdaptor<T5>
{
    public static void Adapt(RowCursor cursor, ref (T1, T2, T3, T4, T5) value)
    {
        var valCursor = new ValueCursor(cursor);
        TAdaptor1.Adapt(valCursor, ref value.Item1!);
        valCursor.ColumnIndex++;
        TAdaptor2.Adapt(valCursor, ref value.Item2!);
        valCursor.ColumnIndex++;
        TAdaptor3.Adapt(valCursor, ref value.Item3!);
        valCursor.ColumnIndex++;
        TAdaptor4.Adapt(valCursor, ref value.Item4!);
        valCursor.ColumnIndex++;
        TAdaptor5.Adapt(valCursor, ref value.Item5!);
    }
}

public class TupleAdaptor<T1, T2, T3, T4, T5, T6, TAdaptor1, TAdaptor2, TAdaptor3, TAdaptor4, TAdaptor5, TAdaptor6> : IRowAdaptor<(T1, T2, T3, T4, T5, T6)>
    where TAdaptor1 : IValueAdaptor<T1>
    where TAdaptor2 : IValueAdaptor<T2>
    where TAdaptor3 : IValueAdaptor<T3>
    where TAdaptor4 : IValueAdaptor<T4>
    where TAdaptor5 : IValueAdaptor<T5>
    where TAdaptor6 : IValueAdaptor<T6>
{
    public static void Adapt(RowCursor cursor, ref (T1, T2, T3, T4, T5, T6) value)
    {
        var valCursor = new ValueCursor(cursor);
        TAdaptor1.Adapt(valCursor, ref value.Item1!);
        valCursor.ColumnIndex++;
        TAdaptor2.Adapt(valCursor, ref value.Item2!);
        valCursor.ColumnIndex++;
        TAdaptor3.Adapt(valCursor, ref value.Item3!);
        valCursor.ColumnIndex++;
        TAdaptor4.Adapt(valCursor, ref value.Item4!);
        valCursor.ColumnIndex++;
        TAdaptor5.Adapt(valCursor, ref value.Item5!);
        valCursor.ColumnIndex++;
        TAdaptor6.Adapt(valCursor, ref value.Item6!);
    }
}

public class TupleAdaptor<T1, T2, T3, T4, T5, T6, T7, TAdaptor1, TAdaptor2, TAdaptor3, TAdaptor4, TAdaptor5, TAdaptor6, TAdaptor7> : IRowAdaptor<(T1, T2, T3, T4, T5, T6, T7)>
    where TAdaptor1 : IValueAdaptor<T1>
    where TAdaptor2 : IValueAdaptor<T2>
    where TAdaptor3 : IValueAdaptor<T3>
    where TAdaptor4 : IValueAdaptor<T4>
    where TAdaptor5 : IValueAdaptor<T5>
    where TAdaptor6 : IValueAdaptor<T6>
    where TAdaptor7 : IValueAdaptor<T7>
{
    public static void Adapt(RowCursor cursor, ref (T1, T2, T3, T4, T5, T6, T7) value)
    {
        var valCursor = new ValueCursor(cursor);
        TAdaptor1.Adapt(valCursor, ref value.Item1!);
        valCursor.ColumnIndex++;
        TAdaptor2.Adapt(valCursor, ref value.Item2!);
        valCursor.ColumnIndex++;
        TAdaptor3.Adapt(valCursor, ref value.Item3!);
        valCursor.ColumnIndex++;
        TAdaptor4.Adapt(valCursor, ref value.Item4!);
        valCursor.ColumnIndex++;
        TAdaptor5.Adapt(valCursor, ref value.Item5!);
        valCursor.ColumnIndex++;
        TAdaptor6.Adapt(valCursor, ref value.Item6!);
        valCursor.ColumnIndex++;
        TAdaptor7.Adapt(valCursor, ref value.Item7!);
    }
}

public class TupleAdaptor<T1, T2, T3, T4, T5, T6, T7, T8, TAdaptor1, TAdaptor2, TAdaptor3, TAdaptor4, TAdaptor5, TAdaptor6, TAdaptor7, TAdaptor8> : IRowAdaptor<(T1, T2, T3, T4, T5, T6, T7, T8)>
    where TAdaptor1 : IValueAdaptor<T1>
    where TAdaptor2 : IValueAdaptor<T2>
    where TAdaptor3 : IValueAdaptor<T3>
    where TAdaptor4 : IValueAdaptor<T4>
    where TAdaptor5 : IValueAdaptor<T5>
    where TAdaptor6 : IValueAdaptor<T6>
    where TAdaptor7 : IValueAdaptor<T7>
    where TAdaptor8 : IValueAdaptor<T8>
{
    public static void Adapt(RowCursor cursor, ref (T1, T2, T3, T4, T5, T6, T7, T8) value)
    {
        var valCursor = new ValueCursor(cursor);
        TAdaptor1.Adapt(valCursor, ref value.Item1!);
        valCursor.ColumnIndex++;
        TAdaptor2.Adapt(valCursor, ref value.Item2!);
        valCursor.ColumnIndex++;
        TAdaptor3.Adapt(valCursor, ref value.Item3!);
        valCursor.ColumnIndex++;
        TAdaptor4.Adapt(valCursor, ref value.Item4!);
        valCursor.ColumnIndex++;
        TAdaptor5.Adapt(valCursor, ref value.Item5!);
        valCursor.ColumnIndex++;
        TAdaptor6.Adapt(valCursor, ref value.Item6!);
        valCursor.ColumnIndex++;
        TAdaptor7.Adapt(valCursor, ref value.Item7!);
        valCursor.ColumnIndex++;
        TAdaptor8.Adapt(valCursor, ref value.Item8!);
    }
}

public class TupleAdaptorFactory : IRowAdaptorFactory
{
    public bool TryExtractElementTypes(ReadOnlySpan<Result.ColumnInfo> result, Type resultType, out Type[] elementTypes, out int priority)
    {
        priority = 0;
        elementTypes = [];
        if (!resultType.IsAssignableTo(typeof(ITuple)))
            return false;
        
        if (resultType.TryExtractGenericInterfaceArguments(typeof(ValueTuple<,>), out elementTypes)) 
            return true;
        if (resultType.TryExtractGenericInterfaceArguments(typeof(ValueTuple<,,>), out elementTypes))
            return true;
        if (resultType.TryExtractGenericInterfaceArguments(typeof(ValueTuple<,,,>), out elementTypes))
            return true;
        if (resultType.TryExtractGenericInterfaceArguments(typeof(ValueTuple<,,,,>), out elementTypes))
            return true;
        if (resultType.TryExtractGenericInterfaceArguments(typeof(ValueTuple<,,,,,>), out elementTypes))
            return true;
        if (resultType.TryExtractGenericInterfaceArguments(typeof(ValueTuple<,,,,,,>), out elementTypes))
            return true;
        if (resultType.TryExtractGenericInterfaceArguments(typeof(ValueTuple<,,,,,,,>), out elementTypes))
            return true;
        return false;
    }

    public Type CreateType(Type resultType, Type[] elementTypes, Type[] elementAdaptors)
    {
        if (elementTypes.Length == 2)
            return typeof(TupleAdaptor<,,,>).MakeGenericType(elementTypes[0], elementTypes[1], elementAdaptors[0], elementAdaptors[1]);
        if (elementTypes.Length == 3)
            return typeof(TupleAdaptor<,,,,,>).MakeGenericType(elementTypes[0], elementTypes[1], elementTypes[2], elementAdaptors[0], elementAdaptors[1], elementAdaptors[2]);
        if (elementTypes.Length == 4)
            return typeof(TupleAdaptor<,,,,,,,>).MakeGenericType(elementTypes[0], elementTypes[1], elementTypes[2], elementTypes[3], elementAdaptors[0], elementAdaptors[1], elementAdaptors[2], elementAdaptors[3]);
        if (elementTypes.Length == 5)
            return typeof(TupleAdaptor<,,,,,,,,,>).MakeGenericType(elementTypes[0], elementTypes[1], elementTypes[2], elementTypes[3], elementTypes[4], elementAdaptors[0], elementAdaptors[1], elementAdaptors[2], elementAdaptors[3], elementAdaptors[4]);
        if (elementTypes.Length == 6)
            return typeof(TupleAdaptor<,,,,,,,,,,,>).MakeGenericType(elementTypes[0], elementTypes[1], elementTypes[2], elementTypes[3], elementTypes[4], elementTypes[5], elementAdaptors[0], elementAdaptors[1], elementAdaptors[2], elementAdaptors[3], elementAdaptors[4], elementAdaptors[5]);
        if (elementTypes.Length == 7)
            return typeof(TupleAdaptor<,,,,,,,,,,,,,>).MakeGenericType(elementTypes[0], elementTypes[1], elementTypes[2], elementTypes[3], elementTypes[4], elementTypes[5], elementTypes[6], elementAdaptors[0], elementAdaptors[1], elementAdaptors[2], elementAdaptors[3], elementAdaptors[4], elementAdaptors[5], elementAdaptors[6]);
        if (elementTypes.Length == 8)
            return typeof(TupleAdaptor<,,,,,,,,,,,,,,,>).MakeGenericType(elementTypes[0], elementTypes[1], elementTypes[2], elementTypes[3], elementTypes[4], elementTypes[5], elementTypes[6], elementTypes[7], elementAdaptors[0], elementAdaptors[1], elementAdaptors[2], elementAdaptors[3], elementAdaptors[4], elementAdaptors[5], elementAdaptors[6], elementAdaptors[7]);
        throw new NotImplementedException();
    }
}
