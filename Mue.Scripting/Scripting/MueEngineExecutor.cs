using System;
using System.Collections.Generic;

namespace Mue.Scripting
{
    public record MueEngineExecutor(
        string CommandString,
        string RunBy,
        string ThisScript
    )
    {
        public string? CommandArgs { get; init; }
        public IReadOnlyDictionary<string, string>? CommandParams { get; init; }
        public Action<object>? Callback { get; init; }
    }
}