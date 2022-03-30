using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mue.Common.Models;
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

    public delegate Task ExecCommand(GamePlayer player, LocalCommand command);

    public partial class BuiltinCommands : IBuiltinCommands
    {
        protected const string MSG_NO_TARGET = "I don't know what you mean.";
        protected const string MSG_NOTFOUND_PARENT = "Could not find the specified parent.";
        protected const string MSG_NOTFOUND_ACTION = "Could not find the specified action.";
        protected const string MSG_NOTFOUND_LOCATION = "Could not find the specified location.";
        protected const string MSG_NOTFOUND_ENTITY = "I couldn't find what you were talking about.";

        private ILogger<BuiltinCommands> _logger;
        private IWorld _world;
        private List<CommandItem> _cachedCommands;

        public BuiltinCommands(ILogger<BuiltinCommands> logger, IWorld world)
        {
            _logger = logger;
            _world = world;

            _cachedCommands = this.GetType().GetMethods()
                .Select(method => new { method, attrs = method.GetCustomAttributes<BuiltinCommandAttribute>() })
                .Where(w => w.attrs != null && w.attrs.Count() > 0)
                .SelectMany(s => s.attrs.Select(attr => new CommandItem { Method = s.method, Attr = attr }))
                .ToList();
        }

        public ExecCommand FindCommand(string name)
        {
            var targetCmd = _cachedCommands.SingleOrDefault(s => s.Attr.IsPrefix ? name.StartsWith(s.Attr.Action) : s.Attr.Action == name)?.Method;
            if (targetCmd != null)
            {
                return targetCmd.CreateDelegate<ExecCommand>(this);
            }

            return null;
        }
    }

    class CommandItem
    {
        public BuiltinCommandAttribute Attr;
        public MethodInfo Method;
    }
}