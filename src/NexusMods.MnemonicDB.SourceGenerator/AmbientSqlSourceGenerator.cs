using System;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace NexusMods.MnemonicDB.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public class AmbientSqlSourceGenerator : IIncrementalGenerator
{
    
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1) Pick up all AdditionalFiles that end in .sql
            var sqlFiles = context.AdditionalTextsProvider
                .Where(at => at.Path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                // Keep SourceText in the graph (THIS is what gets versioned)
                .Select((at, ct) => new SqlInput()
                    {
                        Path = at.Path,
                        Name = System.IO.Path.GetFileNameWithoutExtension(at.Path),
                        Text = at.GetText(ct)
                    })              // <-- IMPORTANT: pass ct
                .Where(x => x.Text is not null);

            // 2) Generate ONE output per input file (no Collect)
            context.RegisterSourceOutput(sqlFiles, (spc, item) =>
            {
                // Convert SourceText to string only here, at the leaf
                var sql = item.Text!.ToString();

                var model = new AmbientSqlModel
                {
                    Namespace = GetNamespace(sql),
                    Name = item.Name,
                    Sql = CSharpVerbatimString(sql),
                };

                var writer = new System.IO.StringWriter();
                AmbientSqlTemplate.RenderSqlFragment(model, writer);

                // Stable, unique hint name: filename + short hash of full path
                var hint = $"AmbientSql.{Sanitize(item.Name)}.{ShortHash(item.Path)}.g.cs";
                spc.AddSource(hint, SourceText.From(writer.ToString(), Encoding.UTF8));
            });
        }
        
        private static string CSharpVerbatimString(string s)
            => "@\"" + (s ?? string.Empty).Replace("\"", "\"\"") + "\"";

        private static string Sanitize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "_";
            var sb = new StringBuilder(raw.Length);
            var c0 = raw[0];
            sb.Append(char.IsLetter(c0) || c0 == '_' ? c0 : '_');
            for (int i = 1; i < raw.Length; i++)
            {
                var c = raw[i];
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            }
            return sb.ToString();
        }

        private static string ShortHash(string s)
        {
            using var sha1 = SHA1.Create();
            var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(s));
            // 10 hex chars is plenty to avoid collisions in practice
            // manual hex encoding (first 5 bytes -> 10 hex chars)
            var sb = new StringBuilder(10);
            for (int i = 0; i < 5; i++)
                sb.Append(bytes[i].ToString("x2")); // lowercase hex
            return sb.ToString();
        }

        private static string GetNamespace(string sql)
        {
            var lines = sql.Split('\n');
            foreach (var line in lines)
            { 
                var l = line.Trim();
                if (l.StartsWith("-- namespace:"))
                    return l.Substring("-- namespace:".Length).Trim();
            }
            throw new Exception("No namespace found");
        }

        private static string Escape(string s) => s.Replace("\"", "\\\"");
}


record SqlInput
{
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";
    public SourceText? Text { get; set; } = null;
}
