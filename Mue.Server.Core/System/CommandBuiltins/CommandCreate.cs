namespace Mue.Server.Core.System.CommandBuiltins;

public partial class BuiltinCommands
{
    [BuiltinCommand("$createaction")]
    [BuiltinCommand("$createitem")]
    [BuiltinCommand("$createplayer")]
    [BuiltinCommand("$createroom")]
    [BuiltinCommand("$createscript")]
    public async Task Create(GamePlayer player, LocalCommand command)
    {
        string? name = null, targetPassword = null, targetLocation = null, targetParent = null;

        var type = command.Command switch
        {
            "$createaction" => GameObjectType.Action,
            "$createitem" => GameObjectType.Item,
            "$createplayer" => GameObjectType.Player,
            "$createroom" => GameObjectType.Room,
            "$createscript" => GameObjectType.Script,
            _ => GameObjectType.Invalid
        };

        if (type == GameObjectType.Invalid)
        {
            await _world.PublishMessage(MSG_INVALID_CMD, player);
            return;
        }

        if (!String.IsNullOrEmpty(command.Args))
        {
            var full = command.Args;
            var spl = full.Split("=");
            name = spl[0];
            if (spl.Length > 1)
            {
                targetPassword = spl[1];
                targetLocation = spl[1];
            }
        }
        else if (command.Params?.Count > 0)
        {
            name = command.Params["name"];
            targetPassword = command.Params["password"];
            targetLocation = command.Params["location"];
            targetParent = command.Params["parent"];
        }

        if (String.IsNullOrWhiteSpace(name))
        {
            await _world.PublishMessage($"Command {command.Command} was missing a name.", player);
            return;
        }

        IGameObject? parent = null, location = null;

        if (!String.IsNullOrEmpty(targetParent))
        {
            var result = await player.ResolveTarget(targetParent);
            if (result == null)
            {
                await _world.PublishMessage(MSG_NOTFOUND_PARENT, player);
                return;
            }

            if (GameObjectConsts.ParentTypes[type].Contains(result.ObjectType))
            {
                parent = await _world.GetObjectById(result);
            }

            if (parent == null)
            {
                await _world.PublishMessage($"Target [{result.Id}] is not a valid parent.", player);
                return;
            }
        }
        else
        {
            switch (type)
            {
                case GameObjectType.Item:
                    parent = player;
                    break;
                case GameObjectType.Player:
                    parent = await _world.GetRootRoom();
                    break;
                case GameObjectType.Room:
                    parent = await _world.GetRootRoom();
                    break;
            }
        }

        if (!String.IsNullOrEmpty(targetLocation))
        {
            var result = await player.ResolveTarget(targetLocation);
            if (result == null)
            {
                await _world.PublishMessage(MSG_NOTFOUND_LOCATION, player);
                return;
            }

            if (GameObjectConsts.LocationTypes[type].Contains(result.ObjectType))
            {
                location = await _world.GetObjectById(result);
            }

            if (location == null)
            {
                await _world.PublishMessage($"Target [{result.Id}] is not a valid location.", player);
                return;
            }
        }
        else
        {
            switch (type)
            {
                case GameObjectType.Player:
                    location = await _world.GetStartRoom();
                    break;
                case GameObjectType.Room:
                    // Room locations are option
                    break;
                default:
                    location = player;
                    break;
            }
        }

        IGameObject? newObj = null;
        try
        {
            switch (type)
            {
                case GameObjectType.Action:
                    newObj = await GameAction.Create(_world, name, player.Id, location?.Id);
                    break;
                case GameObjectType.Item:
                    newObj = await GameItem.Create(_world, name, player.Id, location?.Id);
                    break;
                case GameObjectType.Player:
                    if (String.IsNullOrWhiteSpace(targetPassword))
                    {
                        await _world.PublishMessage("Error creating player. No password provided.", player);
                        return;
                    }
                    if (parent == null)
                    {
                        await _world.PublishMessage("Error creating player. No parent found. Please contact an admin.", player);
                        return;
                    }

                    try
                    {
                        newObj = await _world.CommandProcessor.RegisterPlayer(name, targetPassword, player.Id, parent.Id, location?.Id);
                    }
                    catch (CommandException e)
                    {
                        _logger.LogWarning(e, "CommandCreate failed while creating a player");
                        await _world.PublishMessage("Error creating player. " + e.Message, player);
                        return;
                    }
                    break;
                case GameObjectType.Room:
                    if (parent == null)
                    {
                        await _world.PublishMessage("Error creating room. No parent found. Please contact an admin.", player);
                        return;
                    }

                    newObj = await GameRoom.Create(_world, name, player.Id, parent.Id, location?.Id);
                    break;
                case GameObjectType.Script:
                    newObj = await GameScript.Create(_world, name, player.Id, location?.Id);
                    break;
            }

            if (newObj != null)
            {
                if (location != null)
                {
                    await _world.PublishMessage($"Created {name} [{newObj.Id}] in {location.Name} [{location.Id}].", player);
                }
                else
                {
                    await _world.PublishMessage($"Created {name} [{newObj.Id}].", player);
                }
            }
            else
            {
                _logger.LogWarning("CommandCreate hit an empty object response case");
                await _world.PublishMessage("Failed to create.", player);
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "CommandCreate threw an error");
            await _world.PublishMessage($"Problem creating object {e.Message}", player);
        }
    }
}
