namespace Mue.Server.Core.System.CommandBuiltins;

public interface IBuiltinCommands : IDisposable
{
    void Reload();
    ExecCommandInstance? FindCommand(string name);

    // Normally commands are auto-discovered, but unknown is only called internally
    Task Unknown(GamePlayer player, LocalCommand command);
}
