namespace Mue.Server.Core.System.CommandBuiltins;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class BuiltinCommandAttribute : Attribute
{
    public string Action { get; init; }
    public bool IsPrefix { get; init; }
    public string[]? Expressions { get; init; }

    public BuiltinCommandAttribute(string action, bool prefix, params string[] expressions)
    {
        this.Action = action;
        this.IsPrefix = prefix;
        this.Expressions = expressions;
    }

    public BuiltinCommandAttribute(string action, params string[] expressions)
    {
        this.Action = action;
        this.Expressions = expressions;
    }
}
