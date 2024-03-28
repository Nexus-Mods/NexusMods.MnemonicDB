using Argon;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Helpers;

public class ObjectTuple
{
    public EntityId E { get; init; }
    public required string A { get; init; }
    public required object V { get; init; }
    public TxId T { get; init; }

    public bool IsAssert { get; init; }
}

public class ObjectTupleWriter : JsonConverter<ObjectTuple>
{
    public override void WriteJson(JsonWriter writer, ObjectTuple value, JsonSerializer serializer)
    {
        var oldFormatting = writer.Formatting;
        writer.WriteStartArray();
        writer.Formatting = Formatting.None;

        writer.WriteValue(value.E.Value.ToString("x"));

        writer.WriteValue(" " + value.A);

        switch (value.V)
        {
            case EntityId eid:
                writer.WriteValue(eid.Value.ToString("x"));
                break;
            case ulong ul:
                writer.WriteValue(ul.ToString("x"));
                break;
            default:
                writer.WriteValue(value.V.ToString());
                break;
        }

        writer.WriteValue(value.T.Value.ToString("x"));

        writer.WriteValue(value.IsAssert ? "assert" : "retract");

        writer.WriteEndArray();
        writer.Formatting = oldFormatting;
    }

    public override ObjectTuple ReadJson(JsonReader reader, Type type, ObjectTuple? existingValue, bool hasExisting,
        JsonSerializer serializer)
    {
        throw new NotSupportedException();
    }
}
