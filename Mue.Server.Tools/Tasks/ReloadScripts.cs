using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mue.Server.Core.System;

namespace Mue.Server.Tools
{
    public class ReloadScriptsTask : StandardTask
    {
        private readonly ILogger<InitTask> _logger;
        private readonly IWorld _world;

        public ReloadScriptsTask(ILogger<InitTask> logger, IWorld world)
        {
            _logger = logger;
            _world = world;
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            var player1 = await _world.GetRootPlayer();
            if (player1 == null)
            {
                throw new Exception("Could not find player root value. Did you init the server?");
            }

            var room0 = await _world.GetRootRoom();
            if (room0 == null)
            {
                throw new Exception("Could not find root room. Did you init the server?");
            }

            // Load scripts
            var scriptLoader = new ScriptLoader(_world);
            await scriptLoader.UpdateScripts(player1.Id, room0.Id, room0.Id);

            _logger.LogInformation("Code reload complete.");
        }
    }
}