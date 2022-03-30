using System;
using System.Threading.Tasks;
using Mue.Common.Models;
using Mue.Server.Core.Objects;

namespace Mue.Server.Core.System.CommandBuiltins
{
    public partial class BuiltinCommands
    {
        [BuiltinCommand("$quit")]
        public Task Quit(GamePlayer player, LocalCommand command)
        {
            player.Quit("Quit by user request");
            return Task.CompletedTask;
        }
    }
}