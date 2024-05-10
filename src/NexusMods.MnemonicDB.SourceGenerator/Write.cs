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

        _gen.Add($"public partial interface I{model.Name} {{");

        _gen.Add("public static class Attributes {");

        foreach (var attr in model.Attributes)
        {
            _gen.Add($"public static readonly {attr.Type.ToDisplayString()} {attr.Name} = new(\"{model.Namespace}\", \"{attr.Name}\");");
        }

        _gen.Add("}");
        _gen.Add("}");
        _gen.Add("}");
    }
}
