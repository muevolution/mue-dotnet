namespace Mue.Server.Core.System.CommandBuiltins;

public partial class BuiltinCommands
{
    // Command parameters

    public const string COMMON_PARAM_SOURCE = "source";
    public const string COMMON_PARAM_TARGET = "target";
    public const string COMMON_PARAM_KEY = "key";
    public const string COMMON_PARAM_VALUE = "value";

    // Messages

    protected const string MSG_INVALID_CMD = "Invalid command.";
    protected const string MSG_NO_TARGET = "I don't know what you mean.";
    protected const string MSG_NOTFOUND_PARENT = "Could not find the specified parent.";
    protected const string MSG_NOTFOUND_ACTION = "Could not find the specified action.";
    protected const string MSG_NOTFOUND_LOCATION = "Could not find the specified location.";
    protected const string MSG_NOTFOUND_PLAYER = "I couldn't find who you were talking about.";
    protected const string MSG_NOTFOUND_ENTITY = "I couldn't find what you were talking about.";
    protected const string MSG_NOTOWNER = "That does not belong to you.";

}

public static class BuiltinCommandExtensions
{
    public static string? GetTarget(this LocalCommand command)
    {
        if (!String.IsNullOrWhiteSpace(command.Args))
        {
            return command.Args;
        }
        else if (command.Params?.ContainsKey(BuiltinCommands.COMMON_PARAM_TARGET) ?? false)
        {
            return command.Params[BuiltinCommands.COMMON_PARAM_TARGET];
        }
        else
        {
            return null;
        }
    }
}
