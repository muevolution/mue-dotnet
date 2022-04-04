using Mue.Common.ClientServer;
using Mue.Common.Models;
using Mue.Server.Core.ClientServer;
using Mue.Server.Core.System;

namespace Mue.Server.Hubs;

public class MueClientHub : Hub<IServerCommands>, IClientCommands
{
    private readonly IWorld _world;
    readonly MueConnectionManager _connMgr;
    private readonly IServiceProvider _serviceProvider;

    public MueClientHub(IWorld world, MueConnectionManager connMgr, IServiceProvider serviceProvider)
    {
        _world = world;
        _connMgr = connMgr;
        _serviceProvider = serviceProvider;
    }

    public override async Task OnConnectedAsync()
    {
        await Server.OnConnect(Client);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Server.OnDisconnect();
        _connMgr.Connections.Remove(this.Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public Task<OperationResponse> Auth(AuthRequest data) => Server.OnAuthRequest(data);
    public Task<OperationResponse> Command(CommandRequest request) => Server.OnCommand(request);
    public Task<OperationResponse> Echo(string message) => Server.OnEcho(message);
    public async Task<OperationResponse> Disconnect()
    {
        var result = await Server.OnDisconnect();
        return result;
    }

    private IServerToClient Client => GetMueConnection().Client;
    private IClientToServer Server => GetMueConnection().Server;

    private MueConnection GetMueConnection()
    {
        var connId = this.Context.ConnectionId;

        if (_connMgr.Connections.ContainsKey(connId))
        {
            return _connMgr.Connections[connId];
        }
        else
        {
            var conn = new MueConnection(
                connId,
                _serviceProvider.GetRequiredService<IClientToServer>(),
                new MueHubServerToClient(Clients.Caller)
            );

            _connMgr.Connections[this.Context.ConnectionId] = conn;
            return conn;
        }
    }
}

class MueHubServerToClient : IServerToClient
{
    private readonly IServerCommands _caller;

    public MueHubServerToClient(IServerCommands caller)
    {
        _caller = caller;
    }

    public Task SendWelcome(string motd) => _caller.Welcome(motd);
    public Task SendMessage(CommunicationsMessage message, MueCodes code) => _caller.Message(message, code);
    public Task SendEcho(string message) => _caller.Echo(message);
    public Task SendDisconnect(string? reason = null) => _caller.Disconnect(reason);
    public Task SendFatal(string message, MueCodes code) => _caller.Fatal(message, code);
}
