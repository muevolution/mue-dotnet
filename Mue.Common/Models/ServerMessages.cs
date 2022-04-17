using System.Collections.ObjectModel;

namespace Mue.Common.Models;

public record LocalCommand(string Command)
{
    public string? Args { get; init; }
    public IDictionary<string, string>? Params { get; init; }
    public Func<string, IDictionary<string, string>>? InputParser { get; init; }

    public bool IsBare => String.IsNullOrEmpty(Args) && (Params == null || Params.Count < 1);

    public IReadOnlyDictionary<string, string> ParseParamsFromArgs()
    {
        if (Params != null)
        {
            // Parameters already set, return those instead
            return new ReadOnlyDictionary<string, string>(Params);
        }

        if (String.IsNullOrEmpty(Args) || InputParser == null)
        {
            // Didn't get any input
            return new Dictionary<string, string>();
        }

        // Call the parser and return the dictionary
        var parsed = InputParser(Args);
        return new ReadOnlyDictionary<string, string>(parsed);
    }
}

public struct MessageFormats
{
    public string FirstPerson;
    public string ThirdPerson;
}

public record InteriorMessage(string Message) : CommunicationsMessage(Message)
{
    public new MessageFormats? ExtendedFormat;
    public string? Script { get; init; }
}
