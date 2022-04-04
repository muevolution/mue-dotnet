using HandlebarsDotNet;

namespace Mue.Server.Core.Utils;

public record FormattedMessage(string Message)
{
    public IReadOnlyDictionary<string, string>? Substitutions { get; init; }
    public string? Format { get; init; }
}

public interface IWorldFormatter
{
    FormattedMessage Format(string message, IReadOnlyDictionary<string, string> args);
}

public class Formatter : IWorldFormatter
{
    private IWorld _world;
    private IHandlebars _hb;

    public Formatter(IWorld world)
    {
        this._world = world;

        _hb = Handlebars.Create();
        using (_hb.Configure())
        {
            _hb.Configuration.NoEscape = true;

            _hb.RegisterHelper("to_name", (context, arguments) =>
            {
                if (arguments.Length < 1)
                {
                    return String.Empty;
                }

                var objId = new ObjectId(arguments[0].ToString());
                if (!objId.IsAssigned)
                {
                    return "[?]";
                }

                var objTask = _world.GetObjectById(objId);
                Task.WaitAll(objTask);
                var obj = objTask.Result;
                return obj?.Name;
            });
        }
    }

    public FormattedMessage Format(string message, IReadOnlyDictionary<string, string> args)
    {
        // TODO: Memoize this by message
        var tpl = _hb.Compile(message);
        var formatted = tpl(args);

        return new FormattedMessage(formatted);
    }
}
