using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mue.Server.Core.ClientServer;
using Mue.Server.Core.System;

namespace Mue.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorldController : ControllerBase
    {
        private readonly ILogger<WorldController> _logger;
        private readonly IWorld _world;

        public WorldController(ILogger<WorldController> logger, IWorld world)
        {
            _logger = logger;
            _world = world;
        }

        [HttpGet]
        [Route("/")]
        public async Task<string> Hello()
        {
            var playerIds = await _world.GetConnectedPlayerIds();
            var playerObjs = await _world.GetObjectsById(playerIds);
            var playerNames = String.Join(',', playerObjs.Select(s => s.Name));

            return "Hello world!\n\nThe following players are connected: " + playerNames;
        }

        [HttpGet]
        [Route("motd")]
        public string Motd()
        {
            return ServerConnector.MOTD;
        }
    }
}
