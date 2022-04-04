using System;

namespace Mue.Scripting
{
    public class MueEngineBindingException : Exception
    {
        public MueEngineBindingException(string message) : base(message) { }
    }

    public class MueScriptException : Exception
    {
        public MueScriptException(string message, Exception? innerException) : base(message, innerException) { }

        public string[]? ScriptStackTrace { get; set; }
    }

    public class MueScriptEntryMissing : MueScriptException
    {
        public MueScriptEntryMissing() : base("Script is missing an entry point", null) { }
    }

    public class MueScriptInvalidSyntax : MueScriptException
    {
        public MueScriptInvalidSyntax(int line, int col, Exception innerException) : base($"Script error on line {line}:{col}", innerException) { }
    }
}