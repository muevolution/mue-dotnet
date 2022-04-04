namespace Mue.Server.Core.System;

public interface ICommandProcessor
{
    Task<GamePlayer> ProcessLogin(string username, string password);
    Task<GamePlayer> RegisterPlayer(string username, string password, ObjectId? creator = null, ObjectId? parent = null, ObjectId? location = null);
    Task<bool> ProcessCommand(GamePlayer player, CommandRequest request);
}
