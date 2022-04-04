namespace Mue.Server.Core.System.CommandBuiltins;

public partial class BuiltinCommands
{
    [BuiltinCommand("$quit")]
    public Task Quit(GamePlayer player, LocalCommand command)
    {
        player.Quit("Quit by user request");
        return Task.CompletedTask;
    }
}
