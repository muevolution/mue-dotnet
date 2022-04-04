using Mue.Scripting;

namespace Mue.Server.Core.Scripting.Implementation;

public class MueScriptUtils
{
    public MueScriptUtils(IWorld world, MueEngineExecutor executor) { }

    [MueExposedScriptMethod]
    public string CreateTable(IEnumerable<IEnumerable<string>> parms)
    {
        // Clients should really use the raw response when possible (even Telnet),
        //  there's no way to make the stock text version look good

        dynamic output = new DynamicDictionary();

        // This is the worst
        var rows = parms.Select(row => String.Join(" | ", row));
        var tableText = String.Join('\n', rows);

        output.text = tableText;
        output.raw = parms.Select(s => s.ToArray()).ToArray();

        return output;
    }
}
