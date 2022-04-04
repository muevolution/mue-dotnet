namespace Mue.Scripting;

public static class MueScriptExceptionExtensions
{
    public static void SetPythonStack(this MueScriptException ex, ScriptEngine engine)
    {
        ex.ScriptStackTrace = engine.GetService<ExceptionOperations>().FormatException(ex.InnerException).Replace("\r\n", "\n").Split("\n");
    }
}
