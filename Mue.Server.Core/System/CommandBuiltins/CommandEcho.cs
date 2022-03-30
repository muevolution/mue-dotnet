using System;
using System.Threading.Tasks;
using Mue.Common.Models;
using Mue.Server.Core.Objects;

namespace Mue.Server.Core.System.CommandBuiltins
{
    public partial class BuiltinCommands
    {
        [BuiltinCommand("$echo")]
        public async Task Echo(GamePlayer player, LocalCommand command)
        {
            if (!String.IsNullOrEmpty(command.Args))
            {
                await _world.PublishMessage($"Echo: {command.Args}", player);
                return;
            }
            if (command.Params?.Count > 0 && command.Params.ContainsKey("text"))
            {
                await _world.PublishMessage($"Echo: {command.Params["text"]}", player);
                return;
            }

            await _world.PublishMessage("Do you like the sound of your own voice that much?", player);
        }
    }
}