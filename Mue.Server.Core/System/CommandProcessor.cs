using Mue.Scripting;
using Mue.Server.Core.Scripting;
using Mue.Server.Core.System.CommandBuiltins;

namespace Mue.Server.Core.System;

public class CommandProcessor : ICommandProcessor
{
    private ILogger<CommandProcessor> _logger;
    private readonly IWorld _world;
    private readonly IScriptManager _scriptManager;
    private readonly IBuiltinCommands _builtinCommands;

    public CommandProcessor(
        ILogger<CommandProcessor> logger,
        IWorld world,
        IScriptManager scriptManager,
        IBuiltinCommands builtinCommands
    )
    {
        _logger = logger;
        _world = world;
        _scriptManager = scriptManager;
        _builtinCommands = builtinCommands;
    }

    public async Task<GamePlayer> ProcessLogin(string username, string password)
    {
        if (String.IsNullOrWhiteSpace(username))
        {
            throw new CommandException("A username must be provided.");
        }
        if (String.IsNullOrWhiteSpace(password))
        {
            throw new CommandException("A password must be provided.");
        }

        var player = await _world.GetPlayerByName(username);
        if (player == null)
        {
            throw new CommandException("Could not find login user.");
        }

        var passwordTest = player.CheckPassword(password);
        if (!passwordTest)
        {
            throw new CommandException("Invalid password.");
        }

        return player;
    }

    public async Task<GamePlayer> RegisterPlayer(string username, string password, ObjectId? creator = null, ObjectId? parent = null, ObjectId? location = null)
    {
        // TODO: Check server settings for player registration origin

        // TODO: Check username rules
        if (String.IsNullOrWhiteSpace(username))
        {
            throw new CommandException("A username must be provided.");
        }
        if (String.IsNullOrWhiteSpace(password))
        {
            throw new CommandException("A password must be provided.");
        }

        var player = await _world.GetPlayerByName(username);
        if (player != null)
        {
            throw new CommandException("That player already exists.");
        }

        // TODO: Make the defaults configurable
        if (creator == null)
        {
            creator = (await _world.GetRootPlayer()).Id;
        }
        if (parent == null)
        {
            parent = (await _world.GetRootRoom()).Id;
        }
        if (location == null)
        {
            location = (await _world.GetStartRoom()).Id;
        }

        player = await GamePlayer.Create(_world, username, password, creator, parent, location);
        return player;
    }

    public async Task<bool> ProcessCommand(GamePlayer player, CommandRequest request)
    {
        LocalCommand cmd;

        if (!request.IsExpanded)
        {
            var line = request.Command;
            if (String.IsNullOrEmpty(line))
            {
                // We got a blank line. This may be useful somewhere else but not at root
                return false;
            }

            var hasSpace = line.IndexOf(" ");
            if (hasSpace > -1)
            {
                cmd = new LocalCommand(line.Substring(0, hasSpace)) { Args = line.Substring(hasSpace + 1) };
            }
            else
            {
                cmd = new LocalCommand(line);
            }
        }
        else
        {
            cmd = new LocalCommand(request.Command) { Params = request.Params };
        }

        if (String.IsNullOrEmpty(cmd.Command))
        {
            throw new Exception("Null command");
        }

        // Rewrite the command if a hard alias is in use
        cmd = RewriteAliases(cmd);

        // Run hardcoded commands
        var hasEvaluated = await RunBuiltinCommands(player, cmd);
        if (hasEvaluated)
        {
            return true;
        }

        // Search the action tree
        hasEvaluated = await RunActionCommands(player, cmd);
        if (hasEvaluated)
        {
            return true;
        }

        // Send unknown command response
        await _builtinCommands.Unknown(player, cmd);
        return false;
    }

    private LocalCommand RewriteAliases(LocalCommand command)
    {
        if (command.Command.StartsWith("\""))
        {
            var spl = command.Command.Substring(1);
            var execCommand = command with { Command = "say" };

            if (command.Args != null)
            {
                return execCommand with { Args = spl + " " + command.Args };
            }
            else
            {
                return execCommand with { Args = spl };
            }
        }
        else if (command.Command.StartsWith(":"))
        {
            var spl = command.Command.Substring(1);
            var execCommand = command with { Command = "pose" };

            if (command.Args != null)
            {
                return execCommand with { Args = spl + " " + command.Args };
            }
            else
            {
                return execCommand with { Args = spl };
            }
        }

        return command;
    }

    private async Task<bool> RunBuiltinCommands(GamePlayer player, LocalCommand command)
    {
        // Use the builtins command manager
        var cmd = _builtinCommands.FindCommand(command.Command.ToLower());
        if (cmd != null)
        {
            await cmd(player, command);
            return true;
        }

        return false;
    }

    private async Task<bool> RunActionCommands(GamePlayer player, LocalCommand command)
    {
        var actionId = await player.Find<GameAction>(command.Command);
        var action = await _world.GetObjectById<GameAction>(actionId);

        if (action == null || action.Target == null)
        {
            return false;
        }

        var target = await _world.GetObjectById(action.Target);
        if (target == null)
        {
            throw new Exception($"Action was tied to invalid target: {action.Target}");
        }

        if (target is GameRoom)
        {
            // Move the player into the room
            await player.Move(target.Id);
            return true;
        }
        else if (target is GameScript scriptTarget)
        {
            // Execute the script
            try
            {
                await _scriptManager.RunScript(scriptTarget, player, command);
            }
            catch (MueScriptException e)
            {
                _logger.LogError(e, "User script execution threw a scripting error");

                var stack = e.ScriptStackTrace != null ? String.Join("\n", e.ScriptStackTrace) : e.InnerException?.Message;
                var msg = new InteriorMessage($"Error executing script [{scriptTarget.Id}]:\n{stack}")
                {
                    Source = player.Id.Id,
                    Meta = new Dictionary<string, string> {
                            {"Exception", e.ToString()}
                        }
                };

                await player.SendMessage(msg);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "User script execution threw an internal error");
                await player.SendMessage($"Error while trying to execute script [{scriptTarget.Id}]: {e.Message}");
            }
            return true;
        }

        throw new Exception($"Action was tied to something other than a room or script: '{action.Target}'");
    }
}

public class CommandException : GameException
{
    public CommandException(string message) : base("CommandProcessor raised a user level error: " + message, new { CommandResponse = message }) { }
}
