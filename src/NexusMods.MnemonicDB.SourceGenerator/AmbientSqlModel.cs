namespace NexusMods.MnemonicDB.SourceGenerator;

public record AmbientSqlModel
{
    public string Namespace { get; set; } = "";
    public string Name { get; set; } = "";
    public string Sql { get; set; } = "";
}
