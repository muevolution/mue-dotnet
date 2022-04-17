namespace Mue.Server.Core.System.CommandBuiltins;

public partial class BuiltinCommands
{
    [BuiltinCommand("$target", CMD_REGEX_NAMELOCATION)]
    public async Task SetTarget(GamePlayer player, LocalCommand command)
    {
        string? targetAction = null, targetLocation = null;

        var cmdParams = command.ParseParamsFromArgs();
        targetAction = cmdParams.GetValueOrDefault(COMMON_PARAM_NAME);
        targetLocation = cmdParams.GetValueOrDefault(COMMON_PARAM_LOCATION);

        if (String.IsNullOrWhiteSpace(targetAction) || String.IsNullOrWhiteSpace(targetLocation))
        {
            await _world.PublishMessage(MSG_NO_TARGET, player, meta: QuickReasonMeta("name|location missing"));
            return;
        }

        var actionId = await player.ResolveTarget(targetAction);
        var action = await _world.GetObjectById<GameAction>(actionId);
        if (action == null)
        {
            await _world.PublishMessage(MSG_NOTFOUND_ACTION, player, meta: QuickReasonMeta("action was null"));
            return;
        }

        var locationId = await player.ResolveTarget(targetLocation);
        var location = await _world.GetObjectById(locationId);
        if (location == null)
        {
            await _world.PublishMessage(MSG_NOTFOUND_LOCATION, player, meta: QuickReasonMeta("location was null"));
            return;
        }
        if (!GameObjectConsts.AllActionTargets.Contains(location.ObjectType))
        {
            await _world.PublishMessage($"Location [{location.Id}] is not a room or a script.", player, meta: QuickReasonMeta("location invalid target"));
            return;
        }

        await action.SetTarget(location.Id);
        await _world.PublishMessage($"Action {action.Name} [{action.Id}] has been targeted at {location.Name} [{location.Id}].", player);
    }
}
