using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Mue.Common.ClientServer;
using Mue.Common.Models;

namespace Mue.Clients.ClientServer
{
    public class HubClient : IClientCommands, IDisposable
    {
        private readonly HubConnection _hub;
        private readonly IServerCommands _handler;
        private readonly List<IDisposable> _disposers = new List<IDisposable>();

        public HubClient(HubConnection hub, IServerCommands handler)
        {
            _hub = hub;
            _handler = handler;

            ConfigureServerCommands();
        }

        public void Dispose()
        {
            if (_disposers != null)
            {
                _disposers.ForEach(f => f.Dispose());
            }
        }

        /// <summary>Client intent: I am requesting authentication.</summary>
        public Task<OperationResponse> Auth(AuthRequest data) => _hub.InvokeAsync<OperationResponse>("Auth", data);
        /// <summary>Client intent: I am sending a command for the server to execute.</summary>
        public Task<OperationResponse> Command(CommandRequest request) => _hub.InvokeAsync<OperationResponse>("Command", request);
        /// <summary>Client intent: Echo test.</summary>
        public Task<OperationResponse> Echo(string message) => _hub.InvokeAsync<OperationResponse>("Echo", message);
        /// <summary>Client intent: I am going to close the session.</summary>
        public Task<OperationResponse> Disconnect() => _hub.InvokeAsync<OperationResponse>("Disconnect");

        private void ConfigureServerCommands()
        {
            _disposers.AddRange(new List<IDisposable> {
                _hub.On<string>("Welcome", _handler.Welcome),
                // _hub.On<AuthResponse, MueCodes>("AuthResponse", _handler.AuthResponse),
                _hub.On<CommunicationsMessage, MueCodes>("Message", _handler.Message),
                _hub.On<string>("Echo", _handler.Echo),
                _hub.On<string>("Disconnect", _handler.Disconnect),
                _hub.On<string, MueCodes>("Fatal", _handler.Fatal),
            });
        }

    }
}
