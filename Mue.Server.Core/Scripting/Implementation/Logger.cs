using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mue.Scripting;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;
using Mue.Server.Core.System;
using Mue.Server.Core.Utils;

namespace Mue.Server.Core.Scripting.Implementation
{
    public class MueScriptLogger
    {
        private IWorld _world;
        private MueEngineExecutor _executor;

        public MueScriptLogger(IWorld world, MueEngineExecutor executor)
        {
            _world = world;
            _executor = executor;
        }

        [MueExposedScriptMethod]
        public async Task Log(string level, string message, IEnumerable<object> parms)
        {
            var target = await _world.GetObjectById<GamePlayer>(new ObjectId(_executor.RunBy));

            string? json = null;
            if (parms != null)
            {
                json = Json.Serialize(parms);
                json = json.Substring(1, json.Length - 2);
            }

            await this._world.PublishMessage($"Script [{_executor.ThisScript}] log {level}> {message}{(json != null ? ("\n" + json) : String.Empty)}", target, null);
        }
    }
}