using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace Mue.Server.Core.System.CommandBuiltins;

public class InputParser
{
    private IEnumerable<Regex> _regexes = Enumerable.Empty<Regex>();

    public InputParser(IEnumerable<string> regexTexts)
    {
        _regexes = regexTexts.Select(s => new Regex(s, RegexOptions.Compiled | RegexOptions.IgnoreCase)).Reverse();
    }

    public Dictionary<string, string> Match(string args)
    {
        foreach (var re in _regexes)
        {
            var m = re.Match(args);
            if (m.Success)
            {
                var gdic = m.Groups as IEnumerable<KeyValuePair<string, Group>>;
                return gdic.Where(s => s.Value.Success).ToDictionary(k => k.Key, v => v.Value.Value);
            }
            else
            {
                continue;
            }
        }

        return new Dictionary<string, string>();
    }
}
