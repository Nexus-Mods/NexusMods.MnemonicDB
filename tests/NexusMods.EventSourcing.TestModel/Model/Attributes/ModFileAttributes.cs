using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model.Attributes;

public static class ModFileAttributes
{
    public class Hash : ScalarAttribute<Hash, ulong>;

    public class Path : ScalarAttribute<Path, string>;

    public class Index : ScalarAttribute<Index, ulong>;
}
