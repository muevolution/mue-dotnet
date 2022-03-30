using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mue.Common.Models;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;

namespace Mue.Server.Core.System.CommandBuiltins
{
    public partial class BuiltinCommands
    {
        [BuiltinCommand("$inspect")]
        public async Task Inspect(GamePlayer player, LocalCommand command)
        {
            var output = new StringBuilder();
            output.AppendLine($"Player: {player.Name} [{player.Id}]");

            var parent = await _world.GetObjectById(player.Parent);
            if (parent != null)
            {
                output.AppendLine($"Player parent: {parent.Name} [{parent.Id}]");
            }
            else
            {
                output.AppendLine("Player parent: none");
            }

            Func<IEnumerable<ObjectId>, Task<string>> ContentsToStr = async (contents) =>
            {
                var contentsObj = await Task.WhenAll(contents.Select(s => _world.GetObjectById(s)));
                var contentsName = contentsObj.Select(s => s.Name);
                return String.Join(", ", contentsName);
            };

            var playerContents = await player.GetContents();
            if (playerContents.Count() > 0)
            {
                output.AppendLine($"Player contents: [{await ContentsToStr(playerContents)}]");
            }
            else
            {
                output.AppendLine("Player contents: none");
            }

            var location = await _world.GetObjectById(player.Location);
            output.AppendLine($"Player location: {location.Name} [{location.Id}]");

            var locationContents = await (location as IContainer)?.GetContents();
            if (locationContents?.Count() > 0)
            {
                output.AppendLine($"Room contents: [{await ContentsToStr(locationContents)}]");
            }
            else
            {
                output.AppendLine("Room contents: none");
            }

            await _world.PublishMessage(output.ToString(), player);
        }
    }
}