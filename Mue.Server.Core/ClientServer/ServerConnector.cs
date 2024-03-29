using System.Reactive.Linq;
using System.Reactive;
using Mue.Backend.PubSub;
using System.Text;

namespace Mue.Server.Core.ClientServer;

public class ServerConnector : IClientToServer
{
    private readonly ILogger<ServerConnector> _logger;
    private readonly IWorld _world;
    private readonly IBackendPubSub _pubSub;
    private readonly IWorldFormatter _worldFormatter;
    private IServerToClient? _client;
    private GamePlayer? _player;
    private IDisposable? _playerEventSubscription;
    private ISubscriptionToken? _psPlayerLoc;
    private List<ISubscriptionToken> _subTokens = new List<ISubscriptionToken>();

    public bool IsConnected { get; private set; }
    public bool IsAuthenticated => _player != null;

    public ServerConnector(ILogger<ServerConnector> logger, IWorld world, IBackendPubSub pubSub, IWorldFormatter worldFormatter)
    {
        _logger = logger;
        _world = world;
        _pubSub = pubSub;
        _worldFormatter = worldFormatter;
    }

    public async Task OnConnect(IServerToClient client)
    {
        _client = client;
        IsConnected = true;

        var motd = await _world.GetMOTD();
        await _client.SendWelcome(motd);
    }

    public async Task<OperationResponse> OnAuthRequest(AuthRequest data)
    {
        try
        {
            if (data.IsRegistration)
            {
                _player = await _world.CommandProcessor.RegisterPlayer(data.Username, data.Password);
            }
            else
            {
                _player = await _world.CommandProcessor.ProcessLogin(data.Username, data.Password);
            }
        }
        catch (CommandException e)
        {
            _logger.LogWarning(e, "Auth request login processing threw error");
            return new OperationResponse { Success = false, Fatal = true, Message = e.Message, Code = MueCodes.LoginError };
        }

        if (_player == null)
        {
            return new OperationResponse { Success = false, Fatal = true, Message = "Failed to perform login. Contact an administrator.", Code = MueCodes.UnauthenticatedError };
        }

        _subTokens.Add(await _pubSub.Subscribe("c:world", OnPubSubMessage));
        _psPlayerLoc = await _pubSub.Subscribe($"c:{_player.Location}", OnPubSubMessage);
        _subTokens.Add(_psPlayerLoc);
        _subTokens.Add(await _pubSub.Subscribe($"c:{_player.Id}", OnPubSubMessage));

        // Subscribe the player's object to their own player events
        _playerEventSubscription = _world.WorldEventStream.Where(w => w.Id == _player.Id).SelectMany(OnPlayerEvent).Subscribe();

        // Tell the room the player was in
        var playerLocationObj = await _world.GetObjectById<GameRoom>(_player.Location);
        await _world.PublishMessage($"{_player.Name} has connected.", playerLocationObj);

        // Tell everything else
        // Theoretically this could be used instead of telling the room directly
        await _world.FirePlayerEvent(_player.Id, PlayerUpdate.EVENT_CONNECT, new PlayerConnectionResult { RemainingConnections = 0 });

        // Tell the connection
        return new OperationResponse { Message = $"Welcome {_player.Name} [{_player.Id.Id}]" };
    }

    public async Task<OperationResponse> OnCommand(CommandRequest message)
    {
        if (!IsAuthenticated || _player == null)
        {
            return new OperationResponse { Success = false, Fatal = false, Message = "You have not yet authenticated.", Code = MueCodes.UnauthenticatedError };
        }

        try
        {
            await _world.PlayerCommand(_player, message);
            return new OperationResponse();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "User command '{message}' threw exception", message);
            return new OperationResponse { Success = false, Fatal = false, Message = "Exception thrown: " + e.Message, Code = MueCodes.UnknownError };
        }
    }

    public async Task<OperationResponse> OnEcho(string message)
    {
        if (_client == null)
        {
            return new OperationResponse { Success = false };
        }

        await _client.SendEcho(message);
        return new OperationResponse();
    }

    public async Task<OperationResponse> OnDisconnect()
    {
        if (!IsConnected)
        {
            return new OperationResponse { Success = false };
        }

        await Quit();

        await Task.WhenAll(_subTokens.Select(f => f.Unsubscribe()));
        this._playerEventSubscription?.Dispose();

        IsConnected = false;

        return new OperationResponse { Fatal = true, Code = MueCodes.Nothing };
    }

    private async Task OnPubSubMessage(string channel, string message)
    {
        if (_client == null || _player == null)
        {
            return;
        }

        var displayedChannel = channel.Substring(2);
        if (displayedChannel == this._player.Id.Id)
        {
            displayedChannel = "you";
        }

        var msgObj = Json.Deserialize<InteriorMessage>(message);
        if (msgObj == null)
        {
            return;
        }

        var eventTarget = msgObj.Target != null ? msgObj.Target : "message";

        switch (eventTarget)
        {
            case "message":
                var localMsg = GetLocalMessage(msgObj);
                if (localMsg == null || (localMsg.Message == null && localMsg.Format == null))
                {
                    break;
                }

                var extendedContent = GeneralUtils.MergeDicts(msgObj.ExtendedContent, localMsg.Substitutions);

                var output = new CommunicationsMessage(localMsg?.Message ?? msgObj.Message)
                {
                    Source = msgObj.Source,
                    Target = displayedChannel,
                    ExtendedFormat = localMsg?.Format,
                    ExtendedContent = extendedContent,
                    Meta = msgObj.Meta,
                };

                await _client.SendMessage(output, MueCodes.Success);

                break;
            case "echo":
                if (!String.IsNullOrWhiteSpace(msgObj.Message))
                {
                    await _client.SendEcho(msgObj.Message);
                }
                break;
        }
    }

    private async Task Quit(string? reason = null)
    {
        if (_player != null)
        {
            await _world.PublishMessage($"{_player.Name} has disconnected");
        }

        if (_client != null)
        {
            await _client.SendDisconnect(reason ?? "No reason given");
        }
    }

    private async Task<Unit> OnPlayerEvent(ObjectUpdate update)
    {
        switch (update)
        {
            case { EventName: PlayerUpdate.EVENT_QUIT, Meta: QuitResult m }:
                await this.Quit(m.Reason);
                break;
            case { EventName: PlayerUpdate.EVENT_QUIT }:
                await this.Quit();
                break;
            case { EventName: ObjectUpdate.EVENT_MOVE, Meta: MoveResult m }:
                if (!IsConnected)
                {
                    return Unit.Default;
                }

                if (m.OldLocation != null)
                {
                    if (_psPlayerLoc != null)
                    {
                        await _pubSub.Unsubscribe(_psPlayerLoc);
                        _subTokens.Remove(_psPlayerLoc);
                    }
                }

                _psPlayerLoc = await _pubSub.Subscribe($"c:{m.NewLocation}", OnPubSubMessage);
                _subTokens.Add(_psPlayerLoc);
                break;
        }

        return Unit.Default;
    }

    private FormattedMessage GetLocalMessage(InteriorMessage msg)
    {
        if (_player == null)
        {
            // This shouldn't happen but handle it anyway
            return new FormattedMessage(msg.Message);
        }

        if (msg.ExtendedFormat != null && msg.ExtendedContent != null)
        {
            var format = (msg.Source == this._player.Id.Id) ? msg.ExtendedFormat?.FirstPerson : msg.ExtendedFormat?.ThirdPerson;
            if (format != null)
            {
                var formatted = _worldFormatter.Format(format, msg.ExtendedContent);

                return new FormattedMessage(formatted.Message)
                {
                    Substitutions = formatted.Substitutions,
                    Format = format,
                };
            }
        }

        return new FormattedMessage(msg.Message);
    }
}
