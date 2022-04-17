namespace Mue.Server.Core.System.CommandBuiltins;

public partial class BuiltinCommands
{
    private static readonly GameObjectType[] ALLOWED_DROPTAKE_TYPES = new GameObjectType[] { GameObjectType.Action, GameObjectType.Item, GameObjectType.Script };

    [BuiltinCommand("drop", CMD_REGEX_TARGET_WHOLE)]
    [BuiltinCommand("take", CMD_REGEX_TARGET_WHOLE, $"^{CMD_REGEX_TARGET} from {CMD_REGEX_SOURCE}")]
    [BuiltinCommand("put", $"^{CMD_REGEX_SOURCE} in {CMD_REGEX_TARGET}")]
    public async Task DropTake(GamePlayer player, LocalCommand command)
    {
        bool isDrop = command.Command == "drop";
        bool isTake = command.Command == "take";
        bool isPut = command.Command == "put";
        if (!isDrop && !isTake && !isPut)
        {
            await _world.PublishMessage(MSG_INVALID_CMD, player);
            return;
        }

        if (player.Location == null)
        {
            // This probably shouldn't happen
            await _world.PublishMessage(MSG_ILLEGAL_LOCATION, player);
            return;
        }

        // Find whatever it is we're talking about
        var cmdParams = command.ParseParamsFromArgs();

        IGameObject? source = null;
        string? sourceText = cmdParams.GetValueOrDefault(COMMON_PARAM_SOURCE);
        if (!String.IsNullOrEmpty(sourceText))
        {
            var sourceId = await player.ResolveTarget(sourceText);
            if (sourceId == null)
            {
                await _world.PublishMessage(MSG_NO_TARGET, player, meta: QuickReasonMeta("sourceId was null"));
                return;
            }
            else if (!isPut && !(source is IContainer) || !ALLOWED_DROPTAKE_TYPES.Contains(sourceId.ObjectType))
            {
                await _world.PublishMessage(MSG_NO_TOUCH, player, meta: QuickReasonMeta("not put and source not IContainer or not allowed DTType"));
                return;
            }

            source = await _world.GetObjectById(sourceId);
            if (source == null)
            {
                await _world.PublishMessage(MSG_NOTFOUND_ENTITY, player, meta: QuickReasonMeta("source was null"));
                return;
            }
        }

        ObjectId? targetId = null;
        IGameObject? target = null;
        string? targetText = cmdParams.GetValueOrDefault(COMMON_PARAM_TARGET);
        if (String.IsNullOrEmpty(targetText))
        {
            await _world.PublishMessage(MSG_NOTFOUND_ENTITY, player, meta: QuickReasonMeta("targetText was null"));
            return;
        }

        if (source != null && isTake)
        {
            // Take from source inventory
            targetId = await ((IContainer)source).FindIn(targetText);
        }
        else
        {
            targetId = await player.ResolveTarget(targetText);
        }

        if (targetId == null)
        {
            await _world.PublishMessage(MSG_NO_TARGET, player, meta: QuickReasonMeta("targetId was null"));
            return;
        }
        else if (!isTake && !ALLOWED_DROPTAKE_TYPES.Contains(targetId.ObjectType))
        {
            await _world.PublishMessage(MSG_NO_TOUCH, player, meta: QuickReasonMeta("not take and not DTType"));
            return;
        }

        target = await _world.GetObjectById(targetId);
        if (target == null)
        {
            await _world.PublishMessage(MSG_NOTFOUND_ENTITY, player, meta: QuickReasonMeta("target was null"));
            return;
        }

        // TODO: These should be formatted

        if (isDrop)
        {
            if (target.Location != player.Id)
            {
                await _world.PublishMessage("You're not holding that.", player, meta: QuickReasonMeta("target was not player"));
                return;
            }

            await target.Move(player.Location);
            await _world.PublishMessage($"You drop {target.Name}.", player);
        }

        if (isTake)
        {
            if (source == null && target.Location != player.Location)
            {
                await _world.PublishMessage(MSG_NOTHERE, player, meta: QuickReasonMeta("no source and target was not player"));
                return;
            }

            await target.Move(player.Id);
            await _world.PublishMessage($"You pick up {target.Name}.", player);
        }

        if (isPut)
        {
            if (source != null)
            {
                await source.Move(target.Id);
                await _world.PublishMessage($"You put {source.Name} in {target.Name}.", player);
            }
        }
    }
}
