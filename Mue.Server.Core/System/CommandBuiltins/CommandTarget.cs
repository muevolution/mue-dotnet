using System;
using System.Linq;
using System.Threading.Tasks;
using Mue.Common.Models;
using Mue.Server.Core.Objects;

namespace Mue.Server.Core.System.CommandBuiltins
{
    public partial class BuiltinCommands
    {
        [BuiltinCommand("$target")]
        public async Task SetTarget(GamePlayer player, LocalCommand command)
        {
            string? targetAction = null, targetLocation = null;

            if (!String.IsNullOrEmpty(command.Args))
            {
                var spl = command.Args.Split("=");

                targetAction = spl[0];
                if (spl.Length > 1)
                {
                    targetLocation = spl[1];
                }
            }
            else if (command.Params?.Count > 0)
            {
                targetAction = command.Params["action"];
                targetLocation = command.Params["location"];
            }

            if (String.IsNullOrWhiteSpace(targetAction) || String.IsNullOrWhiteSpace(targetLocation))
            {
                await _world.PublishMessage(MSG_NO_TARGET, player);
                return;
            }

            var actionObjId = await player.ResolveTarget(targetAction);
            var actionObj = await _world.GetObjectById<GameAction>(actionObjId);
            if (actionObj == null)
            {
                await _world.PublishMessage(MSG_NOTFOUND_ACTION, player);
                return;
            }

            var locationObjId = await player.ResolveTarget(targetLocation);
            var locationObj = await _world.GetObjectById(locationObjId);
            if (locationObj == null)
            {
                await _world.PublishMessage(MSG_NOTFOUND_LOCATION, player);
                return;
            }
            if (!GameObjectConsts.AllActionTargets.Contains(locationObj.ObjectType))
            {
                await _world.PublishMessage($"Location [{locationObj.Id}] is not a room or a script.", player);
                return;
            }

            await actionObj.SetTarget(locationObj.Id);
            await _world.PublishMessage($"Action {actionObj.Name} [{actionObj.Id}] has been targeted at {locationObj.Name} [{locationObj.Id}].", player);
        }
    }
}