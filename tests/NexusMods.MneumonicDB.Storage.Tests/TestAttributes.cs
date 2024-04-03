using NexusMods.MneumonicDB.Abstractions;

namespace NexusMods.MneumonicDB.Storage.Tests;

public class TestAttributes
{
    public class FileName : Attribute<FileName, string>;

    public class FileHash : Attribute<FileHash, ulong>;

    public class FileUses : Attribute<FileUses, ulong>;

    public class FileArchive : Attribute<FileArchive, EntityId>;

    public class ArchiveName : Attribute<ArchiveName, string>;

    public class ArchiveHash : Attribute<ArchiveHash, ulong>;
}
