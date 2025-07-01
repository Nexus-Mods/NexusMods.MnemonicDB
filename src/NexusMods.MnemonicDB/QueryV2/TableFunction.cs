using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DuckDB.NET.Native;

namespace NexusMods.MnemonicDB.QueryV2;

public abstract class TableFunction
{
    public Type[] ArgumentTypes { get; }
    public Type[] ReturnTypes { get; }
    public string Name { get; }

    public TableFunction(string name, Type[] argumentTypes, Type[] returnTypes)
    {
        ArgumentTypes = argumentTypes;
        ReturnTypes = returnTypes;
        Name = name;
    }

    internal unsafe void Register(DuckDBNativeConnection connection)
    {
        var fn = NativeMethods.TableFunction.DuckDBCreateTableFunction();

        using (var handle = Name.ToUnmanagedString())
        {
            NativeMethods.TableFunction.DuckDBTableFunctionSetName(fn, handle);
        }

        NativeMethods.TableFunction.DuckDBTableFunctionSetBind(fn, &BindNative);
        NativeMethods.TableFunction.DuckDBTableFunctionSetInit(fn, &Init);
        NativeMethods.TableFunction.DuckDBTableFunctionSetFunction(fn, &TableFunctionScan);
        NativeMethods.TableFunction.DuckDBTableFunctionSetExtraInfo(fn, GCHandle.ToIntPtr(GCHandle.Alloc(this)), &DestroyBindData);
        HighPerfBindings.DuckDBTableFunctionSupportsProjectionPushdown(fn, true);
        
        
        var state = NativeMethods.TableFunction.DuckDBRegisterTableFunction(connection, fn);
        
        if (state == DuckDBState.Error)
        {
            throw new InvalidOperationException($"Failed to register table function '{Name}' with DuckDB.");
        }

        NativeMethods.TableFunction.DuckDBDestroyTableFunction(ref fn);

    }

    protected abstract void Write(DuckDBChunkWriter writer, object? state);

    protected abstract void Bind(ref BindInfoWriter arguments);

    internal class BindInfoContainer
    {
        public int ColumnCount { get; set; }
        internal object? _userData;
        
        public BindInfoContainer(int columnCount, object? userData)
        {
            ColumnCount = columnCount;
            _userData = userData;
        }
    }
    
    public unsafe ref struct BindInfoWriter
    {
        private object? _userData;
        private int _columnCount;
        public readonly ReadOnlySpan<DuckDBValue> Arguments;
        private readonly IntPtr _bindInfo;

        internal BindInfoWriter(IntPtr bindInfo, ReadOnlySpan<DuckDBValue> arguments)
        {
            _bindInfo = bindInfo;
            Arguments = arguments;
        }
        
        public void AddColumn(string name, Type type)
        {
            _columnCount += 1;
            using var logicalType = type.ToLogicalType();
            using var nameHandle = name.ToUnmanagedString();
            HighPerfBindings.DuckDBBindAddResultColumn(_bindInfo, nameHandle, logicalType); 
        }
        
        public void SetBindData<T>(T data)
        {
            _userData = data;
        }

        public void AddColumn<T>(string name)
        {
            AddColumn(name, typeof(T));
        }

        internal BindInfoContainer GetContainer()
        {
            return new BindInfoContainer(_columnCount, _userData);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void DestroyBindData(IntPtr bindData)
    {
        var handle = GCHandle.FromIntPtr(bindData);
        (handle.Target as IDisposable)?.Dispose();
        handle.Free();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void Init(IntPtr info)
    {
        var bindInfoHandle = GCHandle.FromIntPtr(HighPerfBindings.DuckDBInitGetBindInfo(info));
        if (bindInfoHandle.Target is not BindInfoContainer bindInfo)
        {
            throw new InvalidOperationException("User defined table function init failed. Bind info is null");
        }
        var columnCount = HighPerfBindings.DuckDBInitGetColumnCount(info);
        
        var mappings = new int[bindInfo.ColumnCount];
        mappings.AsSpan().Fill(-1);
        for (var queryIndex = 0; queryIndex < columnCount; queryIndex++)
        {
            var internalIndex = HighPerfBindings.DuckDBInitGetColumnIndex(info, (uint)queryIndex);
            mappings[internalIndex] = queryIndex;
        }

        var extraInfoHandle = GCHandle.Alloc(mappings, GCHandleType.Normal);
        HighPerfBindings.DuckDBInitSetInitData(info, GCHandle.ToIntPtr(extraInfoHandle), &DestroyBindData);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void BindNative(IntPtr info)
    {
        try
        {
            var handle = GCHandle.FromIntPtr(NativeMethods.TableFunction.DuckDBBindGetExtraInfo(info));

            if (handle.Target is not TableFunction tableFunction)
            {
                throw new InvalidOperationException("User defined table function bind failed. Bind extra info is null");
            }

            var argumentCount = (int)NativeMethods.TableFunction.DuckDBBindGetParameterCount(info);
            Span<DuckDBValue> values = stackalloc DuckDBValue[argumentCount];
            try
            {
                for (var i = 0; i < argumentCount; i++)
                {
                    values[i] = HighPerfBindings.DuckDBBindGetParameter(info, (ulong)i);
                }

                var helper = new BindInfoWriter(info, values);
                tableFunction.Bind(ref helper);

                var container = helper.GetContainer();
                var bindContainerHandle = GCHandle.ToIntPtr(GCHandle.Alloc(container, GCHandleType.Normal));
                NativeMethods.TableFunction.DuckDBBindSetBindData(info, bindContainerHandle, &DestroyBindData);

            }
            finally
            {
                for (var i = 0; i < argumentCount; i++)
                    values[i].Dispose();
            }

        }
        finally
        {
            
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static void TableFunctionScan(IntPtr info, IntPtr chunk)
    {
        try
        {
            var bindPtr = GCHandle.FromIntPtr(NativeMethods.TableFunction.DuckDBFunctionGetBindData(info));
            if (bindPtr.Target is not BindInfoContainer bindData)
            {
                throw new InvalidOperationException("User defined table function scan failed. Bind data is null");
            }
            var extraInfo = GCHandle.FromIntPtr(NativeMethods.TableFunction.DuckDBFunctionGetExtraInfo(info));

            if (extraInfo.Target is not TableFunction tableFunction)
            {
                throw new InvalidOperationException("User defined table function failed. Function bind data is null");
            }

            var initData = GCHandle.FromIntPtr(HighPerfBindings.DuckDBFunctionGetInitData(info));
            if (initData.Target is not int[] mappings)
            {
                throw new InvalidOperationException("User defined table function scan failed. Init data is null");
            }
            
            var writer = new DuckDBChunkWriter(new DuckDBDataChunk(chunk), mappings);
            tableFunction.Write(writer, bindData._userData);
            
        }
        catch (Exception ex)
        {
            using var message = ex.Message.ToUnmanagedString();
            NativeMethods.TableFunction.DuckDBFunctionSetError(info, message);
        }
    }
}
