using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Mue.Backend;
using Mue.Server.Core.System;

namespace Mue.Server.Hosts
{
    public class HostedWorld : IHostedService
    {
        private readonly IWorld _world;
        private readonly RedisBackend _backend;
        private Timer _announceTimer;

        public HostedWorld(IWorld world, RedisBackend backend)
        {
            _world = world;
            _backend = backend;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _backend.Connect();
            await _world.Init();

            _announceTimer = new Timer(TimeAnnouncer, null, 10000, 60000);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _announceTimer.DisposeAsync();

            await _world.Shutdown();
            await _backend.Disconnect();
            await _backend.DisposeAsync();
        }

        private void TimeAnnouncer(object state)
        {
            _ = DoTimeAnnounce();
        }

        private async Task DoTimeAnnounce()
        {
            var time = DateTime.Now.ToLocalTime().ToString();
            await _world.PublishMessage($"The time is {time}");
        }
    }
}