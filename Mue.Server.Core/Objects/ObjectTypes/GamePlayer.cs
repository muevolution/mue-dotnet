namespace Mue.Server.Core.Objects;

public record PlayerMetadata : ObjectMetadata
{
    public string? PasswordHash { get; init; }
}

public class GamePlayer : Container<PlayerMetadata>
{
    public static Task<GamePlayer> Create(IWorld world, string name, string password, ObjectId creator, ObjectId parent, ObjectId? location = null)
    {
        // Hash the password
        var passwordHash = Security.HashPassword(password);

        var p = new GamePlayer(world, new PlayerMetadata
        {
            Name = name,
            Creator = creator,
            Parent = creator,
            Location = location ?? parent ?? null,
            PasswordHash = passwordHash,
        });

        return world.ObjectCache.StandardCreate(p);
    }

    public static Task<GamePlayer> RootCreate(IWorld world, string name)
    {
        var player0 = new ObjectId("p:0");
        var room0 = new ObjectId("r:0");

        var p = new GamePlayer(world, new PlayerMetadata
        {
            Name = name,
            Creator = player0,
            Parent = room0,
            Location = room0,
        }, player0);

        return world.ObjectCache.StandardCreate(p);
    }

    public static Task<GamePlayer> Imitate(IWorld world, ObjectId id)
    {
        return world.ObjectCache.StandardImitate<GamePlayer, PlayerMetadata>(id, (meta) => Task.FromResult(new GamePlayer(world, meta, id)));
    }

    protected GamePlayer(IWorld world, PlayerMetadata meta, ObjectId? id = null) : base(world, GameObjectType.Player, meta, id)
    {
        _useDeepSearch = true;
    }

    public override Task<ReparentResult?> Reparent(ObjectId newParent)
    {
        return base.Reparent(newParent, new[] { GameObjectType.Room });
    }

    public override async Task<MoveResult?> Move(ObjectId newLocation)
    {
        var result = await base.Move(newLocation);
        if (result == null)
        {
            return null;
        }

        // Notify rooms of change
        // TODO: Make sure the current user doesn't get the third person join/part messages
        if (result.OldLocation != null)
        {
            var oldLocationObj = await _world.GetObjectById(result.OldLocation);
            await _world.PublishMessage($"{Name} has left.", oldLocationObj);
        }

        var newLocationObj = await _world.GetObjectById(result.NewLocation);
        if (newLocationObj == null)
        {
            await SendMessage($"Failed to find your destination [{newLocation}].");
            return null;
        }

        await SendMessage($"You arrive in {newLocationObj.Name}");
        await _world.PublishMessage($"{Name} has arrived.", newLocationObj);

        return result;
    }

    public override Task<ObjectId?> Find(string term, GameObjectType? type = null)
    {
        return Find(term, type, false);
    }

    public async Task<ObjectId?> Find(string term, GameObjectType? type = null, bool searchLoc = false)
    {
        // Search on player first
        var firstSearch = await FindIn(term, type);
        if (firstSearch != null)
        {
            return firstSearch;
        }

        // Now search the player tree
        IContainer? parent = null;
        if (type.HasValue && type == GameObjectType.Action)
        {
            parent = await _world.GetObjectById(Parent) as IContainer;
            if (parent != null)
            {
                var pRes = await parent.Find(term, type);
                if (pRes != null)
                {
                    return pRes;
                }
            }
        }

        // Now search the room tree
        if ((type.HasValue && type == GameObjectType.Action) || searchLoc)
        {
            var location = await _world.GetObjectById(Location) as IContainer;
            if (location == null || location == parent)
            {
                // Already searched this tree
                return null;
            }

            return await location.FindIn(term, type);
        }

        return null;
    }

    /// <summary>Arbitrary target search, usually for a command.</summary>
    public async Task<ObjectId?> ResolveTarget(string target, bool absolute = false)
    {
        if (target == "me")
        {
            return Id;
        }
        else if (target == "here")
        {
            return Location;
        }
        else if (target == "parent")
        {
            return Parent;
        }

        if (absolute)
        {
            // Try direct addressing first
            try
            {
                var targetId = new ObjectId(target);
                var targetObj = await _world.GetObjectById(targetId);
                if (targetObj != null)
                {
                    return targetObj.Id;
                }
            }
            catch (IllegalObjectIdConstructorException)
            {
                // Swallow
                // TODO: We should either refactor the test or handle specific exception only
            }

            var targetPlayer = await _world.GetPlayerByName(target);
            if (targetPlayer != null)
            {
                return targetPlayer.Id;
            }
        }

        return await Find(target, null, true);
    }

    // User stuff

    public Task<bool> SendMessage(string message)
    {
        return _world.PublishMessage(message, this);
    }

    public Task<bool> SendMessage(InteriorMessage message)
    {
        return _world.PublishMessage(message, this);
    }

    public void Quit(string? reason = null)
    {
        _world.FirePlayerEvent(this.Id, PlayerUpdate.EVENT_QUIT, new QuitResult(reason));
    }

    public bool CheckPassword(string password)
    {
        if (String.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        if (String.IsNullOrWhiteSpace(Meta.PasswordHash))
        {
            return false;
        }

        return Security.ComparePasswords(Meta.PasswordHash, password);
    }
}
