using Newtonsoft.Json.Linq;
using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;
using NexusMods.MnemonicDB.Tests.Resources.Rows;

namespace NexusMods.MnemonicDB.Tests.Resources;

public static class ModlistParser
{
    public static readonly Inlet<JToken> ParsedToken = new();
    
    public static readonly IQuery<ArchiveRow> Archives = 
        from topToken in ParsedToken
        from archive in topToken["Archives"]!
        select ArchiveRow.Parse(archive);

    public static readonly IQuery<DirectiveRow> Directives = 
        from topToken in ParsedToken
        from directive in topToken["Directives"]!
        let parsed = DirectiveRow.Parse(directive)
        where parsed != null
        select parsed;
    
    public static readonly IQuery<(DirectiveRow Directive, string Mod)> DirectiveWithModName =
        from directive in Directives
        let parts = directive.To.Parts
        where parts.First() == "mods" && parts.Count() > 2
        select (directive, parts.Skip(1).First().ToString());
}
