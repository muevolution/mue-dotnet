using System;
using System.Collections.Generic;

namespace Mue.Scripting
{
    public record MueEngineExecutor
    {
        public string CommandString { get; init; }
        public string CommandArgs { get; init; }
        public IReadOnlyDictionary<string, string> CommandParams { get; init; }
        public string RunBy { get; init; }
        public string ThisScript { get; init; }
        public Action<object> Callback { get; init; }
    }
}