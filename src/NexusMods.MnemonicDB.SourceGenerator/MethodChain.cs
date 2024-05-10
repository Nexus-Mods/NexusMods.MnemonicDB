namespace NexusMods.MnemonicDB.SourceGenerator;

public record MethodChain()
{
    public string Namespace { get; set; } = "";
    public MethodCall[] Methods { get; set; } = [];
}
