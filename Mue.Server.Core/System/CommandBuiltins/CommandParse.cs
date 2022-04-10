using Newtonsoft.Json;

namespace Mue.Server.Core.System.CommandBuiltins;

public partial class BuiltinCommands
{
    [BuiltinCommand("$parse")]
    public async Task Parse(GamePlayer player, LocalCommand command)
    {
        if (String.IsNullOrWhiteSpace(command.Args))
        {
            await _world.PublishMessage(MSG_INVALID_CMD, player);
            return;
        }

        CommandRequest newCmd;

        var spacePos = command.Args.IndexOf(' ');
        if (spacePos < 0)
        {
            newCmd = new CommandRequest(command.Args.Trim());
        }
        else
        {
            var cmdText = command.Args.Substring(0, spacePos).Trim();
            var cmdParms = command.Args.Substring(spacePos).Trim();

            try
            {
                var parsedJson = Json.Deserialize<Dictionary<string, string>>(cmdParms);
                newCmd = new CommandRequest(cmdText) { Params = parsedJson, IsExpanded = true };
            }
            catch (JsonException e)
            {
                _logger.LogDebug(e, "$parse threw error");
                await _world.PublishMessage($"Got error while parsing command argument: " + e.Message, player);
                return;
            }
        }

        if (newCmd.Command == "$parse")
        {
            await _world.PublishMessage("Nice try.", player);
            return;
        }

        // Call command system again
        await _world.PlayerCommand(player, newCmd);
    }
}
