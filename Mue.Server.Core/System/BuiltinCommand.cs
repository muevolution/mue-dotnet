using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mue.Common.Models;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;

namespace Mue.Server.Core.System.CommandBuiltins
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class BuiltinCommandAttribute : Attribute
    {
        public string Action { get; init; }
        public bool IsPrefix { get; init; }

        public BuiltinCommandAttribute(string action, bool prefix = false)
        {
            this.Action = action;
            this.IsPrefix = prefix;
        }
    }

    public class BuiltinSubscriber : Attribute
    {
        public Type? UpdateType { get; init; }
        public List<string> SubscriptionTerms { get; init; } = new List<string>();

        public BuiltinSubscriber() { }

        public BuiltinSubscriber(params string[] events)
        {
            SubscriptionTerms.AddRange(events);
        }

        public BuiltinSubscriber(Type updateType, params string[] events)
        {
            UpdateType = updateType;
            SubscriptionTerms.AddRange(events);
        }
    }

    public delegate Task ExecCommand(GamePlayer player, LocalCommand command);
    public delegate Task<Unit> ConsumeUpdate(ObjectUpdate update);

    public partial class BuiltinCommands : IBuiltinCommands, IDisposable
    {
        protected const string MSG_NO_TARGET = "I don't know what you mean.";
        protected const string MSG_NOTFOUND_PARENT = "Could not find the specified parent.";
        protected const string MSG_NOTFOUND_ACTION = "Could not find the specified action.";
        protected const string MSG_NOTFOUND_LOCATION = "Could not find the specified location.";
        protected const string MSG_NOTFOUND_PLAYER = "I couldn't find who you were talking about.";
        protected const string MSG_NOTFOUND_ENTITY = "I couldn't find what you were talking about.";

        private ILogger<BuiltinCommands> _logger;
        private IWorld _world;
        private List<CommandItem> _cachedCommands;
        private List<IDisposable> _subscriptions;

        public BuiltinCommands(ILogger<BuiltinCommands> logger, IWorld world)
        {
            _logger = logger;
            _world = world;

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
                .SelectMany(s => s.bica.Select(attr => new CommandItem(s.method, attr)))
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

        public ExecCommand? FindCommand(string name)
        {
            var targetCmd = _cachedCommands.SingleOrDefault(s => s.Attr.IsPrefix ? name.StartsWith(s.Attr.Action) : s.Attr.Action == name).Method;
            if (targetCmd != null)
            {
                return targetCmd.CreateDelegate<ExecCommand>(this);
            }

            return null;
        }

        public void Dispose()
        {
            _subscriptions.ForEach(f => f.Dispose());
        }
    }

    record struct CommandItem(MethodInfo Method, BuiltinCommandAttribute Attr);
}