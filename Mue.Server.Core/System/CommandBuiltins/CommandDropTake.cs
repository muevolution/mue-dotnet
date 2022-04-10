namespace Mue.Server.Core.System.CommandBuiltins;

// TODO: 'take X from Y'
// TODO: 'put X in Y'

public partial class BuiltinCommands
{
    private static readonly GameObjectType[] ALLOWED_DROPTAKE_TYPES = new GameObjectType[] { GameObjectType.Action, GameObjectType.Item, GameObjectType.Script };

    [BuiltinCommand("drop")]
    [BuiltinCommand("take")]
    public async Task DropTake(GamePlayer player, LocalCommand command)
    {
        bool isDrop = command.Command == "drop";
        bool isTake = command.Command == "take";
        if (!isDrop && !isTake)
        {
            await _world.PublishMessage(MSG_INVALID_CMD, player);
            return;
        }

        if (player.Location == null)
        {
            // This probably shouldn't happen
            await _world.PublishMessage("I don't know where you are.", player);
            return;
        }

        // Find whatever it is we're talking about
        ObjectId? targetId = null;
        IGameObject? target = null;

        string? targetText = command.GetTarget();
        if (targetText != null)
        {
            targetId = await player.ResolveTarget(targetText);
        }

        if (targetId == null)
        {
            await _world.PublishMessage(MSG_NO_TARGET, player);
            return;
        }

        if (!ALLOWED_DROPTAKE_TYPES.Contains(targetId.ObjectType))
        {
            await _world.PublishMessage("You can't touch that.", player);
            return;
        }

        target = await _world.GetObjectById(targetId);
        if (target == null)
        {
            await _world.PublishMessage(MSG_NOTFOUND_ENTITY, player);
            return;
        }

        // TODO: These should be formatted
        if (isDrop)
        {
            if (target.Location != player.Id)
            {
                await _world.PublishMessage("You're not holding that.", player);
                return;
            }

            await target.Move(player.Location);
            await _world.PublishMessage($"You drop {target.Name}.", player);
        }

        if (isTake)
        {
            if (target.Location != player.Location)
            {
                await _world.PublishMessage("You don't see that here.", player);
                return;
            }

            await target.Move(player.Id);
            await _world.PublishMessage($"You pick up {target.Name}.", player);
        }
    }
}
