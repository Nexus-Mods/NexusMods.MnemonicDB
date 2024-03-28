using NexusMods.MneumonicDB.Abstractions;

namespace NexusMods.MneumonicDB.Storage.Tests;

public class TestAttributes
{
    public class FileName : ScalarAttribute<FileName, string>;

    public class FileHash : ScalarAttribute<FileHash, ulong>;

    public class FileUses : ScalarAttribute<FileUses, ulong>;

    public class FileArchive : ScalarAttribute<FileArchive, EntityId>;

    public class ArchiveName : ScalarAttribute<ArchiveName, string>;

    public class ArchiveHash : ScalarAttribute<ArchiveHash, ulong>;
}
