using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DynamicData;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

using MemberDefinition = (ParameterInfo Ref, ParameterInfo Base, ParameterExpression Variable, ISerializer Serializer);

public sealed class BinaryEventSerializer : IEventSerializer
{
    private readonly PooledMemoryBufferWriter _writer;

    private readonly Dictionary<Type, EventSerializerDelegate> _serializerDelegates = new();

    /// <summary>
    /// Write an event to the given writer, and return the
    /// </summary>
    internal delegate void EventSerializerDelegate(IEvent @event);

    public BinaryEventSerializer(IEnumerable<ISerializer> diInjectedSerializers, IEnumerable<EventDefinition> eventDefinitions)
    {
        _writer = new PooledMemoryBufferWriter();
        PopulateSerializers(diInjectedSerializers.ToArray(), eventDefinitions.Where(t => t.Type.Name == "SimpleTestEvent").ToArray());
    }

    private void PopulateSerializers(ISerializer[] diInjectedSerializers, EventDefinition[] eventDefinitions)
    {
        foreach (var eventDefinition in eventDefinitions)
        {
            _serializerDelegates[eventDefinition.Type] = MakeSerializer(eventDefinition, diInjectedSerializers);
        }
    }


    public ReadOnlySpan<byte> Serialize(IEvent @event)
    {
        _writer.Reset();
        _serializerDelegates[@event.GetType()](@event);
        return _writer.GetWrittenSpan();
    }

    public IEvent Deserialize(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }


    private EventSerializerDelegate MakeSerializer(EventDefinition definition, ISerializer[] serializers)
    {
        var deconstructParams = definition.Type.GetMethod("Deconstruct")?.GetParameters().ToArray()!;
        var ctorParams = definition.Type.GetConstructors()
            .First(c => c.GetParameters().Length == deconstructParams.Length)
            .GetParameters();

        var paramDefinitions = deconstructParams.Zip(ctorParams)
            .Select(p => (p.First, p.Second, Expression.Variable(p.Second.ParameterType, p.Second.Name), serializers.First(s => s.CanSerialize(p.Second.ParameterType))))
            .ToArray();


        SortParams(paramDefinitions, out var isFixedSize, out var fixedSize, out var fixedParams, out var unfixedParams);

        if (isFixedSize)
        {
            return BuildFixedSizeSerializer(definition, fixedParams, fixedSize);
        }

        throw new NotImplementedException();

    }

    private static void SortParams(MemberDefinition[] paramDefinitions, out bool isFixedSize, out int fixedSize, out List<MemberDefinition> fixedParams, out List<MemberDefinition> unfixedParams)
    {
        fixedParams = new List<MemberDefinition>();
        unfixedParams = new List<MemberDefinition>();

        fixedSize = 0;

        foreach (var definition in paramDefinitions.OrderBy(p => p.Base.Name))
        {
            if (definition.Serializer.TryGetFixedSize(definition.Base.ParameterType, out var size))
            {
                fixedParams.Add(definition);
                fixedSize += size;
            }
            else
            {
                unfixedParams.Add(definition);
            }
        }

        isFixedSize = unfixedParams.Count == 0;
    }

    internal static Span<byte> SliceFastStart(Span<byte> data, int start, int to)
    {
        return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(data), start),
            data.Length - start);
    }

    internal static Span<byte> SliceFastStartLength(Span<byte> data, int start, int length)
    {
        return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(data), start), length);
    }

    private MethodInfo _sliceFastStartLengthMethodInfo =
        typeof(BinaryEventSerializer).GetMethod(nameof(SliceFastStartLength), BindingFlags.Static | BindingFlags.NonPublic)!;


    internal Expression MakeWindowExpress(Expression span, int offset, int size)
    {
        return Expression.Call(null, _sliceFastStartLengthMethodInfo, span, Expression.Constant(offset),
            Expression.Constant(size));
    }

    private EventSerializerDelegate BuildFixedSizeSerializer(EventDefinition eventDefinition, List<MemberDefinition> definitions, int fixedSize)
    {
        // This function effectively generates:
        // void Serialize(IEvent @event)
        // {
        //    var span = _writer.GetSpan(7);
        //    ((TEvent) event).Deconstruct(out var a, out var b, out var c);
        //    uint8Serializer.Serialize(in a, span.SliceFast(0, 1));
        //    uint32Serializer.Serialize(in b, span.SliceFast(1, 4));
        //    uint16Serializer.Serialize(in c, span.SliceFast(5, 7));
        //    _writer.Advance(7);
        // }


        var inputParam = Expression.Parameter(typeof(IEvent));

        var converted = Expression.Convert(inputParam, eventDefinition.Type);

        var writerParam = Expression.Constant(_writer);

        var spanParam = Expression.Call(writerParam, "GetSpan", null, [Expression.Constant(fixedSize)]);

        var block = new List<Expression>();

        var callDeconstructExpr = Expression.Call(converted, "Deconstruct", null, definitions.Select(d => d.Variable).ToArray());

        block.Add(callDeconstructExpr);


        var offset = 0;
        foreach (var definition in definitions)
        {

            var method = definition.Serializer.GetType().GetMethod("Serialize")!;

            definition.Serializer.TryGetFixedSize(definition.Base.ParameterType, out var size);

            // Reduce the size of the span, so serializers don't need to do their own offsets
            var windowed = MakeWindowExpress(spanParam, offset, size);
            var expr = Expression.Call(Expression.Constant(definition.Serializer), method, [definition.Variable, windowed]);
            block.Add(expr);
        }

        var advanceCall = Expression.Call(writerParam, "Advance", null, Expression.Constant(fixedSize));
        block.Add(advanceCall);

        var allParams = new List<ParameterExpression>
        {
            inputParam
        };

        var blockExpr = Expression.Block(definitions.Select(d => d.Variable), block);

        var lambda = Expression.Lambda<EventSerializerDelegate>(blockExpr, allParams);
        return lambda.Compile();
    }


}
