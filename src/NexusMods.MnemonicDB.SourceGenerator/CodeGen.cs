using System.Text;

namespace NexusMods.MnemonicDB.SourceGenerator;

public class CodeGen
{
    private int _indent;
    private StringBuilder _builder = new();

    public void Add(params string[] lines)
    {
        var line = string.Join("", lines);
        if (line == "}")
            _indent--;
        _builder.Append(new string(' ', _indent * 4));
        _builder.AppendLine(line);
        if (line.EndsWith("{"))
            _indent++;
    }

    public string Build()
    {
        return _builder.ToString();
    }
}
