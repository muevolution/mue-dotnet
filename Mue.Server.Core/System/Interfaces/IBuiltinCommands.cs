using System.Threading.Tasks;
using Mue.Common.Models;
using Mue.Server.Core.Objects;

namespace Mue.Server.Core.System.CommandBuiltins
{
    public interface IBuiltinCommands
    {
        ExecCommand? FindCommand(string name);

        // Normally commands are auto-discovered, but unknown is only called internally
        Task Unknown(GamePlayer player, LocalCommand command);
    }
}