namespace Mue.Server.Core.System.CommandBuiltins;

public partial class BuiltinCommands
{
    private static readonly GameObjectType[] ALLOWED_RECYCLE_TYPES = new GameObjectType[] { GameObjectType.Action, GameObjectType.Item, GameObjectType.Room, GameObjectType.Script };

    [BuiltinCommand("$recycle", CMD_REGEX_TARGET_WHOLE)]
    public async Task Recycle(GamePlayer player, LocalCommand command)
    {
        ObjectId? targetId = null;
        IGameObject? target = null;

        string? targetText = command.ParseParamsFromArgs().GetValueOrDefault(COMMON_PARAM_TARGET);
        if (targetText != null)
        {
            targetId = await player.ResolveTarget(targetText);
        }

        if (targetId == null)
        {
            await _world.PublishMessage(MSG_NO_TARGET, player);
            return;
        }

        if (!ALLOWED_RECYCLE_TYPES.Contains(targetId.ObjectType))
        {
            await _world.PublishMessage("You can't recycle that.", player);
            return;
        }

        target = await _world.GetObjectById(targetId);
        if (target == null)
        {
            await _world.PublishMessage(MSG_NOTFOUND_ENTITY, player);
            return;
        }

        // Safety check - don't let another player recycle something that isn't theirs
        if (target.MetaBasic.Creator != player.Id)
        {
            await _world.PublishMessage(MSG_NOTOWNER, player);
            return;
        }

        // Dummy check - don't let the player erase root elements
        var rootRoom = await _world.GetRootRoom();
        var startRoom = await _world.GetStartRoom();
        if (target == rootRoom || target == startRoom)
        {
            await _world.PublishMessage($"Object '{target.Name}' [{target.Id}] is forbidden.", player);
            return;
        }

        var cachedName = target.Name;
        await target.Destroy();
        await _world.PublishMessage($"Recycled object '{cachedName}'", player);
    }
}
