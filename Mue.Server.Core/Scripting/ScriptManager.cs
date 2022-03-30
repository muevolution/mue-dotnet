using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mue.Common.Models;
using Mue.Scripting;
using Mue.Server.Core.Objects;
using Mue.Server.Core.System;

namespace Mue.Server.Core.Scripting
{
    public class ScriptManager : IScriptManager
    {
        private IWorld _world;
        private IServiceProvider _serviceProvider;

        public ScriptManager(IWorld world, IServiceProvider serviceProvider)
        {
            _world = world;
            _serviceProvider = serviceProvider;
        }

        public async Task RunScript(GameScript script, GamePlayer runBy, LocalCommand command)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<IScriptEngine>();
                var executor = new MueEngineExecutor
                {
                    CommandString = command.Command,
                    CommandArgs = command.Args,
                    CommandParams = command.Params != null ? new Dictionary<string, string>(command.Params) : new Dictionary<string, string>(),
                    RunBy = runBy.Id.Id,
                    ThisScript = script.Id.Id,
                };
                var integration = ScriptIntegration.Build(_world, executor);

                await engine.SpawnAndRun(script.Name, script.Code, 10000, integration);
            }
        }
    }
}