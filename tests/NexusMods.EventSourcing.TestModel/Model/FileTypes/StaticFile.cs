﻿using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model.FileTypes;

[Entity("B4A59C27-22F2-49CE-9685-DDAEF1FCD478")]
public class StaticFile(IEntityContext context, EntityId<StaticFile> id) : AFile(context, id.Id)
{
    public ulong Hash => _hash.Get(this);
    internal static readonly ULongAttribute<StaticFile> _hash = new(nameof(Hash));

    public ulong Size => _size.Get(this);
    internal static readonly ULongAttribute<StaticFile> _size = new(nameof(Size));
}