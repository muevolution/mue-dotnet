using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;

namespace Mue.Server.Tools;

public class ScriptLoader
{
    private readonly IWorld _world;
    private readonly string _scriptDir;

    public ScriptLoader(IConfiguration config, IWorld world)
    {
        _world = world;
        _scriptDir = config["ScriptDir"] ?? @"../Mue.Server.Core/Scripting/Defaults";
    }

    private IEnumerable<string> GetScripts()
    {
        return Directory.GetFiles(_scriptDir, "*.py").Select(s => Path.GetFileName(s));
    }

    private async Task UpdateScript(string filename, ObjectId creator, ObjectId location, ObjectId? actionDestination = null)
    {
        var scriptCreated = false;
        var rootRoom = await _world.GetRootRoom();
        var scriptId = await rootRoom.FindIn<GameScript>(filename);
        var script = await _world.GetObjectById<GameScript>(scriptId);
        if (script == null)
        {
            script = await GameScript.Create(_world, filename, creator, location);
            scriptCreated = true;
        }

        var fileContents = await File.ReadAllLinesAsync($"{_scriptDir}/{filename}");
        await _world.StorageManager.SetScriptCode(script.Id, String.Join('\n', fileContents));

        // TODO: Search and update somehow instead
        if (scriptCreated && actionDestination != null)
        {
            var actionRegex = new Regex(@"^\#\!worldscript ([\w|;]+)$");
            var result = actionRegex.Match(fileContents.First());
            if (result.Groups.Count < 2)
            {
                // Not a script
                return;
            }

            var actionName = result.Groups[1].Value;
            var action = await GameAction.Create(_world, actionName, creator, actionDestination);
            await action.SetTarget(script.Id);
        }
    }

    public async Task UpdateScripts(ObjectId creator, ObjectId location, ObjectId? actionDestination = null)
    {
        var scripts = GetScripts();
        if (scripts.Count() < 1) {
            throw new Exception("No scripts found in script directory");
        }

        foreach (var script in scripts)
        {
            await UpdateScript(script, creator, location, actionDestination);
        }

        await _world.InvalidateScriptCache();
    }
}
