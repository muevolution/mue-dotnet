namespace Mue.Server.Core.System.CommandBuiltins;

public partial class BuiltinCommands
{
    // Command parameters

    public const string COMMON_PARAM_SOURCE = "source";
    public const string COMMON_PARAM_TARGET = "target";
    public const string COMMON_PARAM_KEY = "key";
    public const string COMMON_PARAM_VALUE = "value";
    public const string COMMON_PARAM_NAME = "name";
    public const string COMMON_PARAM_LOCATION = "location";
    public const string COMMON_PARAM_PARENT = "parent";

    // Messages

    protected const string MSG_ILLEGAL_LOCATION = "System error: You do not have a location. Please contact an administrator.";
    protected const string MSG_INVALID_CMD = "Invalid command.";
    protected const string MSG_NO_TARGET = "I don't know what you mean.";
    protected const string MSG_NOTFOUND_PARENT = "Could not find the specified parent.";
    protected const string MSG_NOTFOUND_ACTION = "Could not find the specified action.";
    protected const string MSG_NOTFOUND_LOCATION = "Could not find the specified location.";
    protected const string MSG_NOTFOUND_PLAYER = "I couldn't find who you were talking about.";
    protected const string MSG_NOTFOUND_ENTITY = "I couldn't find what you were talking about.";
    protected const string MSG_NOTOWNER = "That does not belong to you.";
    protected const string MSG_NOTHERE = "You don't see that here.";
    protected const string MSG_NO_TOUCH = "You can't touch that.";

    // Regexes

    protected const string CMD_REGEX_TARGET = @"(?<target>[\w\s]+)";
    protected const string CMD_REGEX_TARGET_WHOLE = $"^{CMD_REGEX_TARGET}$";
    protected const string CMD_REGEX_SOURCE = @"(?<source>[\w\s]+)";
    protected const string CMD_REGEX_NAME = @"(?<name>[\w\s]+)";
    protected const string CMD_REGEX_NAME_WHOLE = $"^{CMD_REGEX_NAME}$";
    protected const string CMD_REGEX_LOCATION = @"(?<location>[\w\s]+)";
    protected const string CMD_REGEX_NAMELOCATION = $"^{CMD_REGEX_NAME}={CMD_REGEX_LOCATION}$";
    protected const string CMD_REGEX_KEY = @"(?<key>[\w\s]+)";
    protected const string CMD_REGEX_KEYVALUE = $"{CMD_REGEX_KEY}=(?<value>[\\w\\s]+)";

    // Help

    protected Dictionary<string, string> QuickReasonMeta(string value)
    {
        return new Dictionary<string, string> {
            {"debug_reason", value},
        };
    }
}
