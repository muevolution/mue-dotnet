namespace Mue.Server.Core.System.CommandBuiltins;

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

        var actionId = await player.ResolveTarget(targetAction);
        var action = await _world.GetObjectById<GameAction>(actionId);
        if (action == null)
        {
            await _world.PublishMessage(MSG_NOTFOUND_ACTION, player);
            return;
        }

        var locationId = await player.ResolveTarget(targetLocation);
        var location = await _world.GetObjectById(locationId);
        if (location == null)
        {
            await _world.PublishMessage(MSG_NOTFOUND_LOCATION, player);
            return;
        }
        if (!GameObjectConsts.AllActionTargets.Contains(location.ObjectType))
        {
            await _world.PublishMessage($"Location [{location.Id}] is not a room or a script.", player);
            return;
        }

        await action.SetTarget(location.Id);
        await _world.PublishMessage($"Action {action.Name} [{action.Id}] has been targeted at {location.Name} [{location.Id}].", player);
    }
}
