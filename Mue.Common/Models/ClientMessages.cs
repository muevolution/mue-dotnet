namespace Mue.Common.Models;

public record GenericResponse(bool Success);

public record ErrorResponse(
    string Message,
    MueCodes Mode
);

public record AuthRequest(
    string Username,
    string Password,
    bool IsRegistration = false
);

public record CommandRequest(string Command)
{
    public bool IsExpanded { get; init; } = false; // If true, only use Command, else Command and Params are split (TODO: Make this smarter?)
    public IDictionary<string, string>? Params { get; init; }
}

public record CommunicationsMessage(string Message)
{
    public const string META_ERRTYPE = "error_type";
    public const string META_ORIGIN = "origin";
    public const string META_RENDERER = "message_renderer";
    public const string META_RENDERER_DEFAULT = "plaintext";
    public const string META_RENDERER_LIST = "list";
    public const string META_RENDERER_TABLE = "table";
    public const string META_RENDERER_CLIENTONLY = "client_only";
    public const string META_TABLE_CONTENT = "table_content";
    public const string META_LIST_CONTENT = "list_content";

    public const string MSG_NO_LISTS = "Your client does not support rendering lists.";
    public const string MSG_NO_TABLES = "Your client does not support rendering tables.";

    public string? Source { get; init; }
    public string? Target { get; init; }
    public IReadOnlyDictionary<string, string>? ExtendedContent { get; init; }
    public string? ExtendedFormat { get; init; }
    public IReadOnlyDictionary<string, string>? Meta { get; init; }

    public static IReadOnlyDictionary<string, string> PurifyMeta(IReadOnlyDictionary<string, string> meta)
    {
        var dict = new Dictionary<string, string>(meta);
        dict.Remove(META_ERRTYPE);
        dict.Remove(META_ORIGIN);
        dict.Remove(META_RENDERER);
        return dict;
    }
}

public record CommunicationsMessage_List(IEnumerable<string> List, string? Message = null);

public record CommunicationsMessage_Table(IEnumerable<IEnumerable<string>> Table, bool HasHeader = false, string? Message = null);
