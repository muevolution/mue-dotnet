using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mue.Common.ClientServer;
using Mue.Common.Models;
using Mue.Backend.PubSub;
using Mue.Server.Core.Objects;
using Mue.Server.Core.System;
using System.Reactive.Linq;
using System.Reactive;
using Mue.Server.Core.Models;
using Mue.Server.Core.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Mue.Server.Core.ClientServer
{
    public class ServerConnector : IClientToServer
    {
        public const string MOTD = @"Welcome to mue (multi-user evolution)! This system is still under development.

  █▀▄▀█   ▄   █      ▄▄▄▄▀ ▄█   ▄      ▄▄▄▄▄   ▄███▄   █▄▄▄▄
  █ █ █    █  █   ▀▀▀ █    ██    █    █     ▀▄ █▀   ▀  █  ▄▀
  █ ▄ █ █   █ █       █    ██ █   █ ▄  ▀▀▀▀▄   ██▄▄    █▀▀▌
  █   █ █   █ ███▄   █     ▐█ █   █  ▀▄▄▄▄▀    █▄   ▄▀ █  █
     █  █▄ ▄█     ▀ ▀       ▐ █▄ ▄█            ▀███▀     █
    ▀    ▀▀▀                   ▀▀▀                      ▀
  ▄███▄      ▄   ████▄ █       ▄     ▄▄▄▄▀ ▄█ ████▄    ▄
  █▀   ▀      █  █   █ █        █ ▀▀▀ █    ██ █   █     █
  ██▄▄   █     █ █   █ █     █   █    █    ██ █   █ ██   █
  █▄   ▄▀ █    █ ▀████ ███▄  █   █   █     ▐█ ▀████ █ █  █
  ▀███▀    █  █            ▀ █▄ ▄█  ▀       ▐       █  █ █
            █▐                ▀▀▀                   █   ██
            ▐

          🚧 This is a development server. Help us develop! 🚧
                       https://github.com/mue/mue-server
";

        private readonly ILogger<ServerConnector> _logger;
        private readonly IWorld _world;
        private readonly IBackendPubSub _pubSub;
        private readonly IWorldFormatter _worldFormatter;
        private IServerToClient _client;
        private GamePlayer _player;
        private IDisposable _playerEventSubscription;
        private ISubscriptionToken _psPlayerLoc;
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

            await _client.SendWelcome(MOTD);
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
            if (!IsAuthenticated)
            {
                return new OperationResponse { Success = false, Fatal = false, Message = "You have not yet authenticated.", Code = MueCodes.UnauthenticatedError };
            }

            await _world.PlayerCommand(_player, message);
            return new OperationResponse();
        }

        public async Task<OperationResponse> OnEcho(string message)
        {
            await _client.SendEcho(message);
            return new OperationResponse();
        }

        public async Task<OperationResponse> OnDisconnect()
        {
            if (!IsConnected)
            {
                return null;
            }

            await Quit();

            await Task.WhenAll(_subTokens.Select(f => f.Unsubscribe()));
            this._playerEventSubscription?.Dispose();

            IsConnected = false;

            return new OperationResponse { Fatal = true, Code = MueCodes.Nothing };
        }

        private async Task OnPubSubMessage(string channel, string message)
        {
            var displayedChannel = channel.Substring(2);
            if (displayedChannel == this._player.Id.Id)
            {
                displayedChannel = "you";
            }

            var msgObj = Json.Deserialize<InteriorMessage>(message);
            var eventTarget = msgObj.Target != null ? msgObj.Target : "message";

            switch (eventTarget)
            {
                case "message":
                    var msgString = GetLocalMessage(msgObj);
                    if (msgString == null || (msgString.Message == null && msgString.Format == null))
                    {
                        break;
                    }

                    var extendedContent = MergeSubstitutions(msgObj.ExtendedContent, msgString.Substitutions);

                    var output = new CommunicationsMessage
                    {
                        Target = displayedChannel,
                        ExtendedFormat = msgString.Format,
                        ExtendedContent = extendedContent,
                        Message = msgString.Message,
                        Source = msgObj.Source,
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

        private async Task Quit(string reason = null)
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
                        await _pubSub.Unsubscribe(_psPlayerLoc);
                        _subTokens.Remove(_psPlayerLoc);
                    }

                    _psPlayerLoc = await _pubSub.Subscribe($"c:{m.NewLocation}", OnPubSubMessage);
                    _subTokens.Add(_psPlayerLoc);
                    break;
            }

            return Unit.Default;
        }

        private IReadOnlyDictionary<string, string> MergeSubstitutions(IReadOnlyDictionary<string, string> dictA, IReadOnlyDictionary<string, string> dictB)
        {
            var outputDict = new Dictionary<string, string>();
            if (dictA != null)
            {
                foreach (var kvp in dictA)
                {
                    outputDict.TryAdd(kvp.Key, kvp.Value);
                }
            }
            if (dictB != null)
            {
                foreach (var kvp in dictB)
                {
                    outputDict.TryAdd(kvp.Key, kvp.Value);
                }
            }
            return outputDict;
        }

        private FormattedMessage GetLocalMessage(InteriorMessage msg)
        {
            if (msg.ExtendedFormat != null && msg.ExtendedContent != null)
            {
                var format = (msg.Source == this._player.Id.Id) ? msg.ExtendedFormat?.FirstPerson : msg.ExtendedFormat?.ThirdPerson;
                var formatted = _worldFormatter.Format(format, msg.ExtendedContent);

                return new FormattedMessage
                {
                    Message = formatted.Message,
                    Substitutions = formatted.Substitutions,
                    Format = format,
                };
            }

            return new FormattedMessage
            {
                Message = msg.Message,
            };
        }
    }
}
