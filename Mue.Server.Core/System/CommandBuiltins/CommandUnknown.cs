using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mue.Common.Models;
using Mue.Server.Core.Objects;
using Mue.Server.Core.Utils;

namespace Mue.Server.Core.System.CommandBuiltins
{
    public partial class BuiltinCommands
    {
        public async Task Unknown(GamePlayer player, LocalCommand command)
        {
            // TODO: Return this as its own event type (or maybe with a flag on message)
            var msg = new InteriorMessage
            {
                Source = player.Id.ToString(),
                Message = $"Unknown command '{command.Command}'",
                Meta = new Dictionary<string, string> {
                    {CommunicationsMessage.META_ERRTYPE, "UNKNOWN_COMMAND"},
                    {"original", Json.Serialize(command)},
                },
            };

            await _world.PublishMessage(msg, player);
        }
    }
}