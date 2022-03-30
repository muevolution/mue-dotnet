using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mue.Common.Models;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;

namespace Mue.Server.Core.System.CommandBuiltins
{
    public partial class BuiltinCommands
    {
        private static readonly Regex SET_ARG_REGEX = new Regex("(.+)=(.+?):(.+)?");

        [BuiltinCommand("$set")]
        public async Task SetProp(GamePlayer player, LocalCommand command)
        {
            // TODO: Use `$set@target key=value` syntax instead of `$set target=key:value`

            string target = null, key = null, value = null;

            if (!String.IsNullOrEmpty(command.Args))
            {
                var reg = SET_ARG_REGEX.Match(command.Args);
                if (reg.Groups.Count == 4)
                {
                    target = reg.Groups[1].Value;
                    key = reg.Groups[2].Value;
                    value = reg.Groups[3].Value;
                }
            }
            else if (command.Params?.Count > 0)
            {
                target = command.Params["target"];
                key = command.Params["key"];
                value = command.Params["value"];
            }

            if (String.IsNullOrWhiteSpace(target) || String.IsNullOrWhiteSpace(key) || String.IsNullOrWhiteSpace(value))
            {
                await _world.PublishMessage(MSG_NO_TARGET, player);
                return;
            }

            var targetObjId = await player.ResolveTarget(target);
            var targetObj = await _world.GetObjectById(targetObjId);
            if (targetObj == null)
            {
                await _world.PublishMessage(MSG_NOTFOUND_ENTITY, player);
                return;
            }

            // TODO: Make this handle numbers and lists
            await targetObj.SetProp(key, new PropValue(value));
            await _world.PublishMessage($"Property '{key}' was set on '{targetObj.Name}' [{targetObj.Id}]", player);
        }
    }
}