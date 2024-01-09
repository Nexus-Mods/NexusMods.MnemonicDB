using System;
using System.Buffers;
using System.Buffers.Binary;
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

public sealed class BinaryEventSerializer : IEventSerializer, IVariableSizeSerializer<IEvent>
{
    private readonly PooledMemoryBufferWriter _writer;

    private readonly Dictionary<Type, EventSerializerDelegate> _serializerDelegates = new();
    private readonly Dictionary<UInt128, EventDeserializerDelegate> _deserializerDelegates = new();

    /// <summary>
    /// Write an event to the given writer, and return the
    /// </summary>
    internal delegate void EventSerializerDelegate(IEvent @event);

    private delegate int EventDeserializerDelegate(ReadOnlySpan<byte> data, out IEvent @event);

    public BinaryEventSerializer(IEnumerable<ISerializer> diInjectedSerializers, IEnumerable<EventDefinition> eventDefinitions)
    {
        _writer = new PooledMemoryBufferWriter();
        PopulateSerializers(diInjectedSerializers.ToArray(), eventDefinitions.ToArray());
    }

    private void PopulateSerializers(ISerializer[] diInjectedSerializers, EventDefinition[] eventDefinitions)
    {
        foreach (var eventDefinition in eventDefinitions)
        {
            var (serializer, deserializer) = MakeSerializer(eventDefinition, diInjectedSerializers);
            _serializerDelegates[eventDefinition.Type] = serializer;
            _deserializerDelegates[eventDefinition.Id] = deserializer;
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
        var id = BinaryPrimitives.ReadUInt128BigEndian(data);
        var used = _deserializerDelegates[id](SliceFastStart(data, 16), out var @event);
        return @event;
    }


    private (EventSerializerDelegate, EventDeserializerDelegate) MakeSerializer(EventDefinition definition, ISerializer[] serializers)
    {
        var deconstructParams = definition.Type.GetMethod("Deconstruct")?.GetParameters().ToArray()!;
        var ctorParams = definition.Type.GetConstructors()
            .First(c => c.GetParameters().Length == deconstructParams.Length)
            .GetParameters();

        var paramDefinitions = deconstructParams.Zip(ctorParams)
            .Select(p => (p.First, p.Second, Expression.Variable(p.Second.ParameterType, p.Second.Name), GetSerializer(serializers, p.Second.ParameterType)))
            .ToArray();


        SortParams(paramDefinitions, out var isFixedSize, out var fixedSize, out var fixedParams, out var unfixedParams);

        if (isFixedSize)
        {
            return (BuildFixedSizeSerializer(definition, paramDefinitions, fixedParams, fixedSize),
                BuildFixedSizeDeserializer(definition, paramDefinitions, fixedParams, fixedSize));
        }

        return (BuildVariableSizeSerializer(definition, paramDefinitions, fixedParams, fixedSize, unfixedParams),
            BuildVariableSizeDeserializer(definition, paramDefinitions, fixedParams, fixedSize, unfixedParams));

    }



    private EventSerializerDelegate BuildVariableSizeSerializer(EventDefinition eventDefinition, MemberDefinition[] allDefinitions,
        List<MemberDefinition> fixedParams, int fixedSize, List<MemberDefinition> unfixedParams)
    {
        // This function effectively generates:
        // void Serialize(IEvent @event)
        // {
        //    var span = _writer.GetSpan(17);
        //    BinaryPrimitives.WriteUInt128BigEndian(span, eventDefinition.Id);
        //    ((TEvent) event).Deconstruct(out byte a, out string strVal);
        //    uint8Serializer.Serialize(in a, span.SliceFast(16, 17));
        //    _writer.Advance(17);
        //
        //    stringSerializer.Serialize<TWriter>(strVal, _writer);
        // }

        var inputParam = Expression.Parameter(typeof(IEvent));

        var converted = Expression.Convert(inputParam, eventDefinition.Type);

        var writerParam = Expression.Constant(_writer);

        var block = new List<Expression>();


        var spanParam = Expression.Variable(typeof(Span<byte>), "span");
        block.Add(Expression.Assign(spanParam, Expression.Call(writerParam, "GetSpan", null, [Expression.Constant(fixedSize + 16)])));

        var callDeconstructExpr = Expression.Call(converted, "Deconstruct", null, allDefinitions.Select(d => d.Variable).ToArray());

        block.Add(callDeconstructExpr);

        var writeIdMethod = typeof(BinaryPrimitives).GetMethod("WriteUInt128BigEndian")!;
        var writeIdExpr = Expression.Call(writeIdMethod, spanParam, Expression.Constant(eventDefinition.Id));

        block.Add(writeIdExpr);

        var offset = 16;
        foreach (var definition in fixedParams)
        {

            var method = definition.Serializer.GetType().GetMethod("Serialize")!;

            definition.Serializer.TryGetFixedSize(definition.Base.ParameterType, out var size);

            // Reduce the size of the span, so serializers don't need to do their own offsets
            var windowed = MakeWindowExpression(spanParam, offset, size);
            var expr = Expression.Call(Expression.Constant(definition.Serializer), method, [definition.Variable, windowed]);
            block.Add(expr);
            offset += size;
        }

        var advanceCall = Expression.Call(writerParam, "Advance", null, Expression.Constant(fixedSize + 16));
        block.Add(advanceCall);

        foreach (var definition in unfixedParams)
        {
            var method = definition.Serializer.GetType().GetMethod("Serialize")!;
            var genericMethod = method.MakeGenericMethod(typeof(PooledMemoryBufferWriter));
            var expr = Expression.Call(Expression.Constant(definition.Serializer), genericMethod, [definition.Variable, writerParam]);
            block.Add(expr);
        }

        var allParams = new List<ParameterExpression>
        {
            inputParam
        };

        var blockExpr = Expression.Block(allDefinitions.Select(d => d.Variable).Append(spanParam), block);

        var lambda = Expression.Lambda<EventSerializerDelegate>(blockExpr, allParams);
        return lambda.Compile();
    }

    private EventDeserializerDelegate BuildVariableSizeDeserializer(EventDefinition definition, MemberDefinition[] allParams,
        List<MemberDefinition> fixedParams, int fixedSize, List<MemberDefinition> unfixedParams)
    {

        var outParam = Expression.Parameter(typeof(IEvent).MakeByRefType(), "output");

        var spanParam = Expression.Parameter(typeof(ReadOnlySpan<byte>));

        var ctorExpressions = new List<Expression>();

        var offsetVariable = Expression.Variable(typeof(int), "offset");

        var blockExprs = new List<Expression>
        {
            Expression.Assign(offsetVariable, Expression.Constant(0))
        };


        var offset = 0;
        foreach (var fixedParam in fixedParams)
        {
            var method = fixedParam.Serializer.GetType().GetMethod("Deserialize")!;

            fixedParam.Serializer.TryGetFixedSize(fixedParam.Base.ParameterType, out var size);

            var windowed = MakeReadonlyWindowExpression(spanParam, offset, size);
            var callExpression = Expression.Call(Expression.Constant(fixedParam.Serializer), method, [windowed]);
            blockExprs.Add(Expression.Assign(fixedParam.Variable, callExpression));
            offset += size;
        }

        blockExprs.Add(Expression.AddAssign(offsetVariable, Expression.Constant(fixedSize)));

        foreach (var unfixedParam in unfixedParams)
        {
            var method = unfixedParam.Serializer.GetType().GetMethod("Deserialize")!;
            var windowed = MakeReadonlyWindowExpression(spanParam, offsetVariable);
            blockExprs.Add(Expression.AddAssign(offsetVariable, Expression.Call(Expression.Constant(unfixedParam.Serializer), method, windowed, unfixedParam.Variable)));
        }

        var ctorParams = allParams.Select(d => d.Variable).ToArray();

        var ctorCall = Expression.New(definition.Type.GetConstructors().First(c => c.GetParameters().Length == ctorParams.Length),
            ctorParams);

        var casted = Expression.Assign(outParam, Expression.Convert(ctorCall, typeof(IEvent)));
        blockExprs.Add(casted);
        blockExprs.Add(offsetVariable);

        var outerBlock = Expression.Block(ctorParams.Append(offsetVariable), blockExprs);
        var lambda = Expression.Lambda<EventDeserializerDelegate>(outerBlock, [spanParam, outParam]);
        return lambda.Compile();
    }

    private ISerializer GetSerializer(ISerializer[] serializers, Type type)
    {
        if (type == typeof(IEvent))
        {
            return this;
        }

        var result = serializers.FirstOrDefault(s => s.CanSerialize(type));
        if (result != null)
        {
            return result;
        }

        if (type.IsConstructedGenericType)
        {
            var genericMakers = serializers.OfType<IGenericSerializer>();
            foreach (var maker in genericMakers)
            {
                if (maker.TrySpecialze(type.GetGenericTypeDefinition(),
                        type.GetGenericArguments(), t => GetSerializer(serializers, t), out var serializer))
                {
                    return serializer;
                }
            }
        }

        if (type.IsArray)
        {
            var arrayMaker = serializers.OfType<GenericArraySerializer>().First();
            arrayMaker.TrySpecialze(type, [type.GetElementType()!], t => GetSerializer(serializers, t), out var serializer);
            return serializer!;
        }

        throw new Exception($"No serializer found for {type}");
    }

    private EventDeserializerDelegate BuildFixedSizeDeserializer(EventDefinition definitions, MemberDefinition[] allDefinitions, List<MemberDefinition> fixedParams, int fixedSize)
    {
        var outParam = Expression.Parameter(typeof(IEvent).MakeByRefType());

        var spanParam = Expression.Parameter(typeof(ReadOnlySpan<byte>));

        var blockExprs = new List<Expression>();

        var offset = 0;
        foreach (var fixedParam in fixedParams)
        {
            var method = fixedParam.Serializer.GetType().GetMethod("Deserialize")!;

            fixedParam.Serializer.TryGetFixedSize(fixedParam.Base.ParameterType, out var size);

            var windowed = MakeReadonlyWindowExpression(spanParam, offset, size);
            var callExpression = Expression.Call(Expression.Constant(fixedParam.Serializer), method, [windowed]);
            blockExprs.Add(Expression.Assign(fixedParam.Variable, callExpression));
            offset += size;
        }

        var ctorCall = Expression.New(definitions.Type.GetConstructors().First(c => c.GetParameters().Length == allDefinitions.Length),
            allDefinitions.Select(d => d.Variable));
        var casted = Expression.Assign(outParam, Expression.Convert(ctorCall, typeof(IEvent)));
        blockExprs.Add(casted);

        blockExprs.Add(Expression.Constant(fixedSize));

        var outerBlock = Expression.Block(allDefinitions.Select(d => d.Variable), blockExprs);
        var lambda = Expression.Lambda<EventDeserializerDelegate>(outerBlock, [spanParam, outParam]);
        return lambda.Compile();

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

    internal static Span<byte> SliceFastStart(ReadOnlySpan<byte> data, int start)
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

    internal static ReadOnlySpan<byte> ReadOnlySliceFastStartLength(ReadOnlySpan<byte> data, int start, int length)
    {
        return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(data), start), length);
    }

    internal static ReadOnlySpan<byte> ReadOnlySliceFastStart(ReadOnlySpan<byte> data, int start)
    {
        return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(data), start), data.Length - start);
    }

    private Expression MakeWindowExpression(Expression span, int offset, int size)
    {
        return Expression.Call(null, _sliceFastStartLengthMethodInfo, span, Expression.Constant(offset),
            Expression.Constant(size));
    }

    private MethodInfo _readonlySliceFastStartLengthMethodInfo =
        typeof(BinaryEventSerializer).GetMethod(nameof(ReadOnlySliceFastStartLength), BindingFlags.Static | BindingFlags.NonPublic)!;

    private Expression MakeReadonlyWindowExpression(Expression span, int offset, int size)
    {
        return Expression.Call(null, _readonlySliceFastStartLengthMethodInfo, span, Expression.Constant(offset),
            Expression.Constant(size));
    }

    private MethodInfo _readonlySliceFastStartMethodInfo =
        typeof(BinaryEventSerializer).GetMethod(nameof(ReadOnlySliceFastStart), BindingFlags.Static | BindingFlags.NonPublic)!;

    private Expression MakeReadonlyWindowExpression(Expression span, int offset)
    {
        return Expression.Call(null, _readonlySliceFastStartMethodInfo, span, Expression.Constant(offset));
    }

    private Expression MakeReadonlyWindowExpression(Expression span, Expression offset)
    {
        return Expression.Call(null, _readonlySliceFastStartMethodInfo, span, offset);
    }

    private EventSerializerDelegate BuildFixedSizeSerializer(EventDefinition eventDefinition, MemberDefinition[] allDefinitions, List<MemberDefinition> definitions, int fixedSize)
    {
        // This function effectively generates:
        // void Serialize(IEvent @event)
        // {
        //    var span = _writer.GetSpan(23);
        //    BinaryPrimitives.WriteUInt128BigEndian(span, eventDefinition.Id);
        //    ((TEvent) event).Deconstruct(out var a, out var b, out var c);
        //    uint8Serializer.Serialize(in a, span.SliceFast(16, 17));
        //    uint32Serializer.Serialize(in b, span.SliceFast(17, 21));
        //    uint16Serializer.Serialize(in c, span.SliceFast(21, 23));
        //    _writer.Advance(23);
        // }


        var inputParam = Expression.Parameter(typeof(IEvent));

        var converted = Expression.Convert(inputParam, eventDefinition.Type);

        var writerParam = Expression.Constant(_writer);

        var block = new List<Expression>();


        var spanParam = Expression.Variable(typeof(Span<byte>), "span");
        block.Add(Expression.Assign(spanParam, Expression.Call(writerParam, "GetSpan", null, [Expression.Constant(fixedSize + 16)])));

        var callDeconstructExpr = Expression.Call(converted, "Deconstruct", null, allDefinitions.Select(d => d.Variable).ToArray());

        block.Add(callDeconstructExpr);

        var writeIdMethod = typeof(BinaryPrimitives).GetMethod("WriteUInt128BigEndian")!;
        var writeIdExpr = Expression.Call(writeIdMethod, spanParam, Expression.Constant(eventDefinition.Id));

        block.Add(writeIdExpr);

        var offset = 16;
        foreach (var definition in definitions)
        {

            var method = definition.Serializer.GetType().GetMethod("Serialize")!;

            definition.Serializer.TryGetFixedSize(definition.Base.ParameterType, out var size);

            // Reduce the size of the span, so serializers don't need to do their own offsets
            var windowed = MakeWindowExpression(spanParam, offset, size);
            var expr = Expression.Call(Expression.Constant(definition.Serializer), method, [definition.Variable, windowed]);
            block.Add(expr);
            offset += size;
        }

        var advanceCall = Expression.Call(writerParam, "Advance", null, Expression.Constant(fixedSize + 16));
        block.Add(advanceCall);

        var allParams = new List<ParameterExpression>
        {
            inputParam
        };

        var blockExpr = Expression.Block(definitions.Select(d => d.Variable).Append(spanParam), block);

        var lambda = Expression.Lambda<EventSerializerDelegate>(blockExpr, allParams);
        return lambda.Compile();
    }


    public bool CanSerialize(Type valueType)
    {
        return valueType == typeof(IEvent);
    }

    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = 0;
        return false;
    }

    public void Serialize<TWriter>(IEvent value, TWriter output) where TWriter : IBufferWriter<byte>
    {
        _serializerDelegates[value.GetType()](value);
    }

    public int Deserialize(ReadOnlySpan<byte> from, out IEvent value)
    {
        var used = _deserializerDelegates[BinaryPrimitives.ReadUInt128BigEndian(from)](SliceFastStart(from, 16), out value);
        return 16 + used;
    }
}
