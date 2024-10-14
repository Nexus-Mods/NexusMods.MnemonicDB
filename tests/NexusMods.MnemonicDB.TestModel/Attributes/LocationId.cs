using TransparentValueObjects;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

[ValueObject<ushort>]
public readonly partial struct LocationId
{
    public static LocationId Game => new(1);
    public static LocationId Saves => new(2);
    public static LocationId Preferences => new(3);
    
    public override string ToString() => Value switch
    {
        1 => "Game",
        2 => "Saves",
        3 => "Preferences",
        _ => "Unknown"
    };
    
}
