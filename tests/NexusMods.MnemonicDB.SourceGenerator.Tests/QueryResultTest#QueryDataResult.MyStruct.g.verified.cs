//HintName: QueryDataResult.MyStruct.g.cs
#nullable enable

public class MyStructAdapterFactory : global::NexusMods.HyperDuck.Adaptor.IRowAdaptorFactory
{
    private static readonly global::System.Type[] ElementTypes = [typeof(string),typeof(string),typeof(string)];

    public bool TryExtractElementTypes(global::System.ReadOnlySpan<global::NexusMods.HyperDuck.Result.ColumnInfo> result, global::System.Type resultType, out global::System.Type[] elementTypes, out int priority)
    {
        if (resultType != typeof(global::NexusMods.MnemonicDB.SourceGenerator.Tests.MyStruct))
        {
            elementTypes = [];
            priority = int.MinValue;
            return false;
        }

        elementTypes = ElementTypes;
        priority = int.MaxValue;
        return true;
    }

    public global::System.Type CreateType(global::System.Type resultType, global::System.Type[] elementTypes, global::System.Type[] elementAdaptors)
    {
        return typeof(MyStructAdapter<,,,>).MakeGenericType(elementAdaptors);
    }
}

public class MyStructAdapter<TParam0Adaptor,TParam1Adaptor,TParam2Adaptor>
    : global::NexusMods.HyperDuck.Adaptor.IRowAdaptor<global::NexusMods.MnemonicDB.SourceGenerator.Tests.MyStruct>
    where TParam0Adaptor : global::NexusMods.HyperDuck.Adaptor.IValueAdaptor<string>
    where TParam1Adaptor : global::NexusMods.HyperDuck.Adaptor.IValueAdaptor<string>
    where TParam2Adaptor : global::NexusMods.HyperDuck.Adaptor.IValueAdaptor<string>
{
    public static bool Adapt(global::NexusMods.HyperDuck.Adaptor.RowCursor cursor, ref global::NexusMods.MnemonicDB.SourceGenerator.Tests.MyStruct value)
    {
        var valueCursor = new global::NexusMods.HyperDuck.Adaptor.ValueCursor(cursor);

        var param0 = value.Foo;
        var param0Eq = TParam0Adaptor.Adapt(valueCursor, ref param0)
        if (!param0Eq) value.Foo = param0;
        valueCursor.ColumnIndex++;

        var param1 = value.Foo;
        var param1Eq = TParam1Adaptor.Adapt(valueCursor, ref param1)
        if (!param1Eq) value.Foo = param1;
        valueCursor.ColumnIndex++;

        var param2 = value.Foo;
        var param2Eq = TParam2Adaptor.Adapt(valueCursor, ref param2)
        if (!param2Eq) value.Foo = param2;
        valueCursor.ColumnIndex++;

        return  param0Eq  ||  param1Eq  ||  param2Eq ;
    }
}
