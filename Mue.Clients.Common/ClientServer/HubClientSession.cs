using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Mue.Common.ClientServer;

namespace Mue.Clients.ClientServer
{
    public class HubClientSession : IAsyncDisposable
    {
        private readonly string _url;
        private readonly IServerCommands _commandHandler;
        private HubConnection? _connection;

        public HubClientSession(string url, IServerCommands commandHandler)
        {
            _url = url;
            _commandHandler = commandHandler;
        }

        public HubClient? Client { get; private set; }
        public bool IsConnected { get { return _connection != null && _connection.State != HubConnectionState.Disconnected; } }

        public async Task Open()
        {
            await OpenHubConnection();
        }

        public async Task Close()
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Close();
            Client?.Dispose();
        }

        private async Task<HubConnection> OpenHubConnection()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(_url)
                .WithAutomaticReconnect()
                .AddNewtonsoftJsonProtocol()
                .Build();

            // TODO: Handle other state changes like reconnecting (notify connected users?)
            _connection.Closed += OnHubClosed;

            Client = new HubClient(_connection, _commandHandler);

            await _connection.StartAsync();

            return _connection;
        }

        private void OnHubError(Exception e)
        {
            // TODO: Notify consumer of hub error
            Console.WriteLine("Hub error: " + e.ToString());
        }

        private Task OnHubClosed(Exception? e)
        {
            // TODO: Handle hub closure
            Console.WriteLine("Hub closed! " + e?.Message);
            return Task.CompletedTask;
        }
    }
}