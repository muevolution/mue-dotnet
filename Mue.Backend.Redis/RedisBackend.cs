using System;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Mue.Backend
{
    public class RedisBackend : IAsyncDisposable
    {
        private readonly string _connectionString;

        public RedisBackend(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async ValueTask DisposeAsync()
        {
            await Disconnect();
            Connection?.Dispose();
        }

        public ConnectionMultiplexer? Connection { get; private set; }

        public async Task Connect()
        {
            Connection = await ConnectionMultiplexer.ConnectAsync(_connectionString);
        }

        public async Task Disconnect()
        {
            if (Connection != null)
            {
                await Connection.CloseAsync();
            }
        }

        public IServer? GetServer() {
            // TODO: This should probably return an enum with all of them
            // We're not clustering right now so it's fine but this is a future warning
            return Connection?.GetServer(Connection?.GetEndPoints()?.First());
        }

        public Task FlushDb() {
            return GetServer()?.FlushDatabaseAsync() ?? Task.CompletedTask;
        }
    }
}