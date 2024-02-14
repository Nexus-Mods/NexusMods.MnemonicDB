using System.Text;

namespace NexusMods.EventSourcing.SourceGenerator;

public class CodeWriter
{
    private readonly StringBuilder _builder = new();
    private int tabCount = 0;

    public void Line(string line)
    {
        if (line.StartsWith("}"))
            tabCount--;
        _builder.AppendLine(new string('\t', tabCount) + line);
        if (line.EndsWith("{"))
            tabCount++;

    }

    public override string ToString() => _builder.ToString();

    public void ClassComment(string comment)
    {
        Line("/// <summary>");
        Line("/// " + comment);
        Line("/// </summary>");
    }

    public void BlankLine()
    {
        Line("");
    }
}
