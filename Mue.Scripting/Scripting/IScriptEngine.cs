namespace Mue.Scripting;

public interface IScriptEngine
{
    Task SpawnAndRun(string scriptName, string script, uint timeout, dynamic binding);
}
