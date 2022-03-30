using System;
using Mue.Scripting;
using Mue.Server.Core.Scripting.Implementation;
using Mue.Server.Core.System;

namespace Mue.Server.Core.Scripting
{
    public static class ScriptIntegration
    {
        public static dynamic Build(IWorld world, MueEngineExecutor executor, bool enableTestMethods = false)
        {
            dynamic output = new DynamicDictionary();

            var context = new ExecutionContext
            {
                Executor = executor,
                WorldType = typeof(IWorld),
                WorldInstance = world,
            };

            output.Script = ScriptIntegrationTools.BuildScript(context);
            output.World = ScriptIntegrationTools.DiscoverMethods<MueScriptWorld>(context);
            output.Logger = ScriptIntegrationTools.DiscoverMethods<MueScriptLogger>(context);
            output.Types = ScriptIntegrationTools.DiscoverMethods<MueScriptTypes>(context);
            output.Utils = ScriptIntegrationTools.DiscoverMethods<MueScriptUtils>(context);

            if (enableTestMethods)
            {
                output.Test = ScriptIntegrationTools.DiscoverMethods<MueScriptTest>(context);
                output.Callback = executor.Callback;
            }

            ((DynamicDictionary)output).Lock();

            return output;
        }
    }
}