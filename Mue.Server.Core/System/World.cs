using Mue.Backend.Storage;
using Mue.Backend.PubSub;
using Mue.Server.Core.Rx;

namespace Mue.Server.Core.System;

public class World : IWorld
{
    private readonly ILogger<World> _logger;
    private readonly ISystemFactory _systemFactory;
    private readonly IBackendStorage _storage;
    private readonly IBackendPubSub _pubSub;
    private bool _hasInit;
    private bool _hasShutdown;

    public string WorldInstanceId { get; private set; }

    public World(
        ILogger<World> logger,
        ISystemFactory systemFactory,
        IBackendStorage storage,
        IBackendPubSub pubSub
    )
    {
        this._logger = logger;
        this._systemFactory = systemFactory;
        this._storage = storage;
        this._pubSub = pubSub;

        this.WorldInstanceId = GeneralUtils.GenerateRandomId();
    }

    public async Task Init()
    {
        if (this._hasInit)
        {
            return;
        }

        this._hasInit = true;
        _logger.LogInformation("ISC> [{thisInstance}] Joining cluster with {activeServers} active servers", WorldInstanceId, await GetActiveServers());
        await ConfigureInterServer();
    }

    public Task Shutdown()
    {
        if (_hasShutdown)
        {
            return Task.CompletedTask;
        }

        _hasShutdown = true;
        _logger.LogInformation("World {thisInstance} shutting down server upon request", WorldInstanceId);

        return Task.CompletedTask;
    }

    public ICommandProcessor CommandProcessor => _systemFactory.CommandProcessor;
    public IStorageManager StorageManager => _systemFactory.StorageManager;
    public IObjectCache ObjectCache => _systemFactory.ObjectCache;

    public Task<bool> PublishMessage(string message, IGameObject? target = null, IDictionary<string, string>? meta = null)
    {
        return PublishMessage(new InteriorMessage(message) { Meta = meta }, target);
    }

    public async Task<bool> PublishMessage(InteriorMessage message, IGameObject? target = null)
    {
        StateEnforce();

        string channel;
        if (target != null)
        {
            var targetId = target.Id;
            if (target is GameItem)
            {
                targetId = target.Parent;
            }

            channel = $"c:{targetId}";
        }
        else
        {
            channel = "c:world";
        }

        var json = Json.Serialize(message);
        _logger.LogDebug("World {thisInstance} publishing message to [{channel}]: {message}", WorldInstanceId, channel, json);
        await _pubSub.Publish(channel, json);
        return true;
    }

    public Task<bool> PlayerCommand(GamePlayer player, CommandRequest command)
    {
        StateEnforce();
        return CommandProcessor.ProcessCommand(player, command);
    }

    public async Task<GamePlayer?> GetPlayerByName(string name)
    {
        StateEnforce();

        var playerId = await StorageManager.FindPlayerByName(name);
        if (playerId == null)
        {
            return null;
        }

        return await GetObjectById<GamePlayer>(new ObjectId(playerId));
    }

    public Task<GamePlayer> GetRootPlayer()
    {
        return GetRootThing<GamePlayer>(RootField.God);
    }

    public Task<GameRoom> GetRootRoom()
    {
        return GetRootThing<GameRoom>(RootField.RootRoom);
    }

    public Task<GameRoom> GetStartRoom()
    {
        return GetRootThing<GameRoom>(RootField.StartRoom);
    }

    private async Task<T> GetRootThing<T>(RootField field) where T : IGameObject
    {
        StateEnforce();

        var id = await StorageManager.GetRootValue(field);
        if (id == null)
        {
            throw new Exception($"Unable to find root key for {field}.");
        }

        var root = await GetObjectById<T>(new ObjectId(id));
        if (root == null)
        {
            throw new Exception($"Unable to find root object for {field} [{id}].");
        }

        return root;
    }

    public async Task<IGameObject?> GetObjectById(ObjectId? id, GameObjectType? assertType = null)
    {
        StateEnforce();

        if (id == null || !id.IsAssigned)
        {
            return null;
        }

        if (assertType != null && id.ObjectType != assertType)
        {
            // TODO: Should this return null?
            return null;
        }

        return (id.ObjectType switch
        {
            GameObjectType.Player => await GamePlayer.Imitate(this, id),
            GameObjectType.Room => await GameRoom.Imitate(this, id),
            GameObjectType.Item => await GameItem.Imitate(this, id),
            GameObjectType.Script => await GameScript.Imitate(this, id),
            GameObjectType.Action => await GameAction.Imitate(this, id),
            _ => null,
        });
    }

    public async Task<T?> GetObjectById<T>(ObjectId? id) where T : IGameObject
    {
        var result = await GetObjectById(id, GameObjectConsts.GetGameObjectType<T>());
        if (result == null)
        {
            return default(T);
        }

        return (T)result;
    }

    public async Task<IEnumerable<T?>> GetObjectsById<T>(IEnumerable<ObjectId?> ids) where T : IGameObject
    {
        StateEnforce();

        var r = await Task.WhenAll(ids.Select(s => GetObjectById<T>(s)));
        return r;
    }

    public async Task<IEnumerable<IGameObject?>> GetObjectsById(IEnumerable<ObjectId?> ids)
    {
        StateEnforce();

        var r = await Task.WhenAll(ids.Select(s => GetObjectById(s)));
        return r;
    }

    public Task<uint> GetActiveServers()
    {
        StateEnforce();

        return _pubSub.GetSubscribeCount("c:isc");
    }

    public async Task<IEnumerable<ObjectId>> GetActiveRoomIds()
    {
        StateEnforce();

        var rooms = await _pubSub.GetTopicsWildcard("c:r:*");
        return rooms.Select(s => new ObjectId(s.Substring(2))).Where(w => w.IsAssigned);
    }

    public async Task<IEnumerable<ObjectId>> GetConnectedPlayerIds()
    {
        StateEnforce();

        var players = await _pubSub.GetTopicsWildcard("c:p:*");
        return players.Select(s => new ObjectId(s.Substring(2))).Where(w => w.IsAssigned);
    }

    public async Task InvalidateScriptCache()
    {
        await ObjectCache.InvalidateAll<GameScript>();
        await SendInterServer(InterServerMessage.CreateInvalidateScript(WorldInstanceId));
    }

    private async Task ConfigureInterServer()
    {
        StateEnforce();

        await SendInterServer(InterServerMessage.CreateJoinedMessage(WorldInstanceId));
        await _pubSub.Subscribe("c:isc", InterServerActivity);
    }

    private Task SendInterServer(InterServerMessage message)
    {
        StateEnforce();

        var json = Json.Serialize(message);
        return _pubSub.Publish("c:isc", json);
    }

    private void InterServerActivity(string topic, string message)
    {
        var msg = Json.Deserialize<InterServerMessage>(message);
        if (msg == null)
        {
            // Ignore empty messages
            return;
        }

        if (msg.InstanceId == WorldInstanceId)
        {
            // Ignore messages from our own instance
            return;
        }

        if (msg.EventName == InterServerMessage.EVENT_JOINED)
        {
            _logger.LogInformation("ISC> [{thisInstance}] New server joined cluster: {originInstance}", WorldInstanceId, msg.InstanceId);
        }
        else if (msg.EventName == InterServerMessage.EVENT_INVALIDATE_SCRIPT)
        {
            _logger.LogInformation("ISC> [{thisInstance}] Script cache invalidate was requested by {originInstance}", WorldInstanceId, msg.InstanceId);
            ObjectCache.InvalidateAll(GameObjectType.Script).ConfigureAwait(false);
        }
        else if (msg.EventName == InterServerMessage.EVENT_UPDATE_OBJECT && msg.Meta != null)
        {
            // TODO: Better handling of meta
            _logger.LogInformation("ISC> [{thisInstance}] Object {objectId} {objectMessage} update requested by {originInstance}", WorldInstanceId, msg.Meta["id"], msg.Meta["message"], msg.InstanceId);
            if (msg.Meta["message"] == "invalidate")
            {
                // Object was changed on another server
                ObjectCache.InvalidateLocal(new ObjectId(msg.Meta["id"]));
            }
            else if (msg.Meta["message"] == "destroyed")
            {
                // Object was destroyed on another server
                ObjectCache.PostNetworkDestroy(new ObjectId(msg.Meta["id"]));
            }
        }
        else if (msg.EventName == InterServerMessage.EVENT_UPDATE_PLAYER && msg.Meta != null)
        {
            if (msg.Meta["message"] == "connect")
            {
                // Player connected to another server
                WorldEventStream.PublishPlayerEvent(new ObjectId(msg.Meta["id"]), msg.Meta["message"], new PlayerConnectionResult
                {
                    RemainingConnections = msg.Meta.ContainsKey("remainingConnections") ? int.Parse(msg.Meta["remainingConnections"]) : -1,
                });
            }
            else if (msg.Meta["message"] == "disconnect")
            {
                // Player disconnected from another server
                WorldEventStream.PublishPlayerEvent(new ObjectId(msg.Meta["id"]), msg.Meta["message"], new PlayerConnectionResult
                {
                    RemainingConnections = msg.Meta.ContainsKey("remainingConnections") ? int.Parse(msg.Meta["remainingConnections"]) : -1,
                });
            }
        }
    }

    public ObjectUpdateObservable WorldEventStream { get; private set; } = new ObjectUpdateObservable();

    public async Task FireObjectEvent<T>(ObjectId id, string eventName, T meta, bool localOnly = false) where T : IObjectUpdateResult
    {
        WorldEventStream.PublishObjectEvent(id, eventName, meta);
        if (!localOnly)
        {
            await SendInterServer(InterServerMessage.CreateObjectUpdate(this.WorldInstanceId, id.Id, eventName));
        }
    }

    public async Task FirePlayerEvent<T>(ObjectId id, string eventName, T meta, bool localOnly = false) where T : IPlayerUpdateResult
    {
        WorldEventStream.PublishPlayerEvent(id, eventName, meta);
        if (!localOnly)
        {
            await SendInterServer(InterServerMessage.CreatePlayerUpdate(this.WorldInstanceId, id.Id, eventName));
        }
    }

    private void StateEnforce()
    {
        if (!_hasInit) throw new WorldNotInitException();
        if (_hasShutdown) throw new WorldShutdownError();
    }
}
