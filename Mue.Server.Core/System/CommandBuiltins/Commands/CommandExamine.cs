using System.Text;

namespace Mue.Server.Core.System.CommandBuiltins;

public partial class BuiltinCommands
{
    [BuiltinCommand("$examine", CMD_REGEX_TARGET_WHOLE)]
    public async Task Examine(GamePlayer player, LocalCommand command)
    {
        var output = new StringBuilder();

        IGameObject? target = null;
        string? targetText = command.ParseParamsFromArgs().GetValueOrDefault(COMMON_PARAM_TARGET);
        if (targetText != null)
        {
            var targetId = await player.ResolveTarget(targetText);
            target = await _world.GetObjectById(targetId);
        }
        else
        {
            target = player;
        }

        if (target == null)
        {
            await _world.PublishMessage(MSG_NOTFOUND_ENTITY, player);
            return;
        }

        output.AppendLine($"{target.ObjectType}: {target.Name} [{target.Id}]");

        var props = await target.GetProps();
        foreach (var prop in props)
        {
            output.AppendLine($" - {prop.Key}: {prop.Value.ToString()}");
        }

        await _world.PublishMessage(output.ToString(), player);
    }
}
