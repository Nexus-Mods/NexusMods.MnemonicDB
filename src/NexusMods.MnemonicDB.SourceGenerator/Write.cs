using System;

namespace NexusMods.MnemonicDB.SourceGenerator;

public class Write
{
    private readonly CodeGen _gen = new();

    public string Build()
    {
        return _gen.Build();
    }

    public void Add(ConcreteModel model)
    {
        _gen.Add($"namespace {model.Namespace} {{");

        // Readonly Model
        _gen.Add($"public partial interface I{model.Name} {{");



        foreach (var attr in model.Attributes)
        {
            _gen.Add("public ", attr.TypeInfo!.HighLevel.ToDisplayString(), " ", attr.Name, " {");
            _gen.Add("get => ", model.Name,".Attributes.", attr.Name, ".Get(this);");
            _gen.Add("}");
        }

        _gen.Add("}");

        // Concrete Model
        _gen.Add("public partial class ", model.Name, " : ", Consts.ModelNamespace,".TempEntity, I", model.Name, " {");

        // Constructor

        _gen.Add($"public {model.Name}(ITransaction tx) {{");
        _gen.Add("Id = tx.TempId();");
        _gen.Add("tx.Attach(this);");
        _gen.Add("}");

        // Attributes
        _gen.Add("public static class Attributes {");
        foreach (var attr in model.Attributes)
        {
            _gen.Add(
                $"public static readonly {attr.Type.ToDisplayString()} {attr.Name} = new(\"{model.Namespace}\", \"{attr.Name}\");");
        }

        _gen.Add("}");

        foreach (var attr in model.Attributes)
        {
            _gen.Add("public required ", attr.TypeInfo!.HighLevel.ToDisplayString(), " ", attr.Name, " { get; init; }");
        }

        _gen.Add("public override void AddTo(ITransaction tx) {");
        _gen.Add("base.AddTo(tx);");
        foreach (var attr in model.Attributes)
        {
            _gen.Add("Attributes.", attr.Name, ".Add(tx, Id!.Value, ", attr.Name, ");");
        }

        _gen.Add("}");

    _gen.Add("}");

    _gen.Add("}");
    }
}
