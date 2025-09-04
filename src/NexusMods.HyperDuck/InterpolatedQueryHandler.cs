using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Reloaded.Memory.Extensions;

namespace NexusMods.HyperDuck;

[InterpolatedStringHandler]
public unsafe ref struct InterpolatedQueryHandler
{
    private static readonly char[] ParamNames = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
    
    private readonly int _scratchSpaceSize;
    private int _literalOffset;
    private readonly byte[] _scratchSpace;
    private object[] _args;
    private int _nextArg;

    public InterpolatedQueryHandler(int literalLength, int parameterCount)
    {
        _scratchSpaceSize = CalculateScratchSpace(literalLength, parameterCount);
        _scratchSpace = new byte[_scratchSpaceSize];
        _literalOffset = 0;
        _nextArg = 0;
        _args = new object[parameterCount];
    }

    private int CalculateScratchSpace(int literalLength, int parameterCount)
    {
        var size = literalLength + (parameterCount * 4) + 1;
        return size;
    }

    public void AppendLiteral(ReadOnlySpan<char> literal)
    {
        for (var i = 0; i < literal.Length; i++)
            _scratchSpace[_literalOffset++] = (byte)literal[i];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void AppendChar(char c)
    {
        _scratchSpace[_literalOffset++] = (byte)c;   
    }
    
    public void AppendFormatted<T>(T arg)
    {
        _args[_nextArg] = arg!;
        AppendChar(' ');
        AppendChar('$');
        AppendChar(ParamNames[_nextArg++]);
        AppendChar(' ');
    }

    public Query<TResult> ToQuery<TResult>(DuckDB engine) where TResult : notnull
    {
        _scratchSpace[_literalOffset++] = 0;
        return new Query<TResult>()
        {
            Sql = new HashedQuery(_scratchSpace),
            Parameters = _args,
            DuckDBQueryEngine = engine,
        };
    }
}
