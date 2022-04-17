namespace Mue.Server.Core.System.CommandBuiltins;

public partial class BuiltinCommands
{
    [BuiltinCommand("$set", true, $"^@{CMD_REGEX_TARGET} {CMD_REGEX_KEYVALUE}$")]
    public async Task SetProp(GamePlayer player, LocalCommand command)
    {
        string? targetStr = null, key = null, value = null;

        var cmdParams = command.ParseParamsFromArgs();
        targetStr = cmdParams.GetValueOrDefault(COMMON_PARAM_TARGET);
        key = cmdParams.GetValueOrDefault(COMMON_PARAM_KEY);
        value = cmdParams.GetValueOrDefault(COMMON_PARAM_VALUE);

        if (String.IsNullOrWhiteSpace(targetStr) || String.IsNullOrWhiteSpace(key) || String.IsNullOrWhiteSpace(value))
        {
            await _world.PublishMessage(MSG_NO_TARGET, player, meta: QuickReasonMeta("target|key|val missing"));
            return;
        }

        var targetId = await player.ResolveTarget(targetStr);
        var target = await _world.GetObjectById(targetId);
        if (target == null)
        {
            await _world.PublishMessage(MSG_NOTFOUND_ENTITY, player, meta: QuickReasonMeta("target was null"));
            return;
        }

        // TODO: Make this handle numbers and lists
        await target.SetProp(key, new PropValue(value));
        await _world.PublishMessage($"Property '{key}' was set on '{target.Name}' [{target.Id}]", player);
    }

    [BuiltinCommand("$unset", true, $"^@{CMD_REGEX_TARGET} {CMD_REGEX_KEY}$")]
    public async Task UnsetProp(GamePlayer player, LocalCommand command)
    {
        string? targetStr = null, key = null;

        var cmdParams = command.ParseParamsFromArgs();
        targetStr = cmdParams.GetValueOrDefault(COMMON_PARAM_TARGET);
        key = cmdParams.GetValueOrDefault(COMMON_PARAM_KEY);

        if (String.IsNullOrWhiteSpace(targetStr) || String.IsNullOrWhiteSpace(key))
        {
            await _world.PublishMessage(MSG_NO_TARGET, player, meta: QuickReasonMeta("target|key missing"));
            return;
        }

        var targetId = await player.ResolveTarget(targetStr);
        var target = await _world.GetObjectById(targetId);
        if (target == null)
        {
            await _world.PublishMessage(MSG_NOTFOUND_ENTITY, player, meta: QuickReasonMeta("target was null"));
            return;
        }

        await target.SetProp(key, new PropValue());
        await _world.PublishMessage($"Property '{key}' was unset on '{target.Name}' [{target.Id}]", player);
    }
}
