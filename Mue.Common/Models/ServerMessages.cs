namespace Mue.Common.Models;

public record LocalCommand(string Command)
{
    public string? Args { get; init; }
    public IDictionary<string, string>? Params { get; init; }

    public bool IsBare => String.IsNullOrEmpty(Args) && (Params == null || Params.Count < 1);
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
