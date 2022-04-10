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
            Parent = parent,
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

    protected GamePlayer(IWorld world, PlayerMetadata meta, ObjectId? id = null) : base(world, GameObjectType.Player, meta, id) { }

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

    /// <summary>Action search, for CommandProcessor.</summary>
    public async Task<ObjectId?> ResolveAction(string term)
    {
        // TODO: This can be sorely optimized, starting with caching which rooms we've already searched through
        // Even better would be to build a full search tree once and invalidate it when the player moves

        // Search on own object contents
        var firstSearch = await FindActionIn(term);
        if (firstSearch != null)
        {
            return firstSearch;
        }

        // Search straight up the parent tree to the root (players can only be parented to rooms)
        var parentSearch = await SearchUpParents(term, Parent);
        if (parentSearch != null)
        {
            return parentSearch;
        }

        // Search in our current location
        var locationObj = await _world.GetObjectById(Location) as IContainer;
        if (locationObj != null)
        {
            var action = await locationObj.FindActionIn(term, true);
            if (action != null)
            {
                return action;
            }

            // Search our current location's parent tree (if our location is a room)
            if (locationObj is GameRoom)
            {
                var locationRoomObj = (GameRoom)locationObj;
                var locationParentSearch = await SearchUpParents(term, locationRoomObj.Parent);
                if (locationParentSearch != null)
                {
                    return locationParentSearch;
                }
            }
        }

        // Search the root room
        var rootRoom = await _world.GetRootRoom();
        var rootSearch = await rootRoom.FindActionIn(term);
        if (rootSearch != null)
        {
            return rootSearch;
        }

        // Not found
        return null;
    }

    /// <summary>Arbitrary target search, usually for a command argument.</summary>
    public async Task<ObjectId?> ResolveTarget(string term)
    {
        switch (term)
        {
            case "me":
                return Id;
            case "here":
                return Location;
            case "parent":
                return Parent;
        }

        // Try direct addressing first
        if (ObjectId.LooksLikeAnId(term))
        {
            try
            {
                var targetId = new ObjectId(term);
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
        }

        var targetPlayer = await _world.GetPlayerByName(term);
        if (targetPlayer != null)
        {
            return targetPlayer.Id;
        }

        // Search on own object contents
        var firstSearch = await FindIn(term);
        if (firstSearch != null)
        {
            return firstSearch;
        }

        // Search in our current location
        var locationObj = await _world.GetObjectById(Location) as IContainer;
        if (locationObj != null)
        {
            var locationSearch = await locationObj.FindIn(term);
            if (locationSearch != null)
            {
                return locationSearch;
            }
        }

        // Not found
        return null;
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

    private async Task<ObjectId?> SearchUpParents(string term, ObjectId startingPoint)
    {
        var current = await _world.GetObjectById<GameRoom>(startingPoint);
        while (current != null)
        {
            var action = await current.FindActionIn(term);
            if (action != null)
            {
                return action;
            }

            if (current.IsParentRoot)
            {
                // Top of parent tree
                break;
            }

            current = await _world.GetObjectById<GameRoom>(current.Parent);
        }

        return null;
    }
}
