using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;

namespace Mue.Server.Core.System.CommandBuiltins;

public partial class BuiltinCommands : IBuiltinCommands, IDisposable
{
    private ILogger<BuiltinCommands> _logger;
    private IWorld _world;
    private List<CommandItem> _cachedCommands = new List<CommandItem>();
    private List<IDisposable> _subscriptions = new List<IDisposable>();

    public BuiltinCommands(ILogger<BuiltinCommands> logger, IWorld world)
    {
        _logger = logger;
        _world = world;

        this.Reload();
    }

    public void Reload()
    {
        // Get all methods and their attributes
        var allMethods = this.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(method => new
            {
                method,
                bica = method.GetCustomAttributes<BuiltinCommandAttribute>(),
                bisub = method.GetCustomAttribute<BuiltinSubscriber>()
            });

        // Cache all available commands
        _cachedCommands = allMethods
            .Where(w => w.bica != null && w.bica.Count() > 0)
            .SelectMany(s => s.bica.Select(attr => new CommandItem(s.method, attr, attr.Expressions != null ? new InputParser(attr.Expressions) : null)))
            .ToList();

        // Subscribe consumers to the world event stream
        _subscriptions = allMethods
            .Where(w => w.bisub != null)
            .Select(s => new { callee = s.method.CreateDelegate<ConsumeUpdate>(this), termFilter = s.bisub?.SubscriptionTerms, typeFilter = s.bisub?.UpdateType })
            .Select(s => _world.WorldEventStream
                .Where(w =>
                    (s.termFilter?.Count > 0 ? s.termFilter.Contains(w.EventName) : true) &&
                    (s.typeFilter != null ? w.GetType().IsAssignableTo(s.typeFilter) : true)
                ).SelectMany(v => s.callee(v)).Subscribe())
            .ToList();

        _logger.LogDebug($"Loaded {_cachedCommands.Count} commands and {_subscriptions.Count} subscriptions");
    }

    public ExecCommandInstance? FindCommand(string name)
    {
        var targetCommand = _cachedCommands.SingleOrDefault(s => s.Attr.IsPrefix ? name.StartsWith(s.Attr.Action) : s.Attr.Action == name);
        if (targetCommand == default(CommandItem))
        {
            return null;
        }

        var targetMethod = targetCommand.Method;
        var cmdDelegate = targetMethod.CreateDelegate<ExecCommand>(this);
        return new ExecCommandInstance(
            targetCommand.Attr.Action,
            cmdDelegate,
            targetCommand.Attr.IsPrefix,
            targetCommand.InputParser
        );
    }

    public void Dispose()
    {
        _subscriptions.ForEach(f => f.Dispose());
    }

    private static InputParser? BuildCommandParser(BuiltinCommandAttribute attr)
    {
        if (attr.Expressions == null || attr.Expressions.Length < 1)
        {
            // No expressions to generate
            return null;
        }

        return new InputParser(attr.Expressions);
    }
}

public record ExecCommandInstance(
    string CommandName,
    ExecCommand CommandDelegate,
    bool PrefixedCommand = false,
    InputParser? InputParser = null
);

public delegate Task ExecCommand(GamePlayer player, LocalCommand command);
public delegate Task<Unit> ConsumeUpdate(ObjectUpdate update);

record struct CommandItem(
    MethodInfo Method,
    BuiltinCommandAttribute Attr,
    InputParser? InputParser
);
