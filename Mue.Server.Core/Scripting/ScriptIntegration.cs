using Mue.Scripting;
using Mue.Server.Core.Scripting.Implementation;

namespace Mue.Server.Core.Scripting;

public static class ScriptIntegration
{
    public static dynamic Build(IWorld world, MueEngineExecutor executor, bool enableTestMethods = false)
    {
        dynamic output = new DynamicDictionary();

        var context = ScriptExecutionContext.CreateContext(executor, world);

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
