using System;
using System.Collections.Generic;
using Mue.Common.ClientServer;

namespace Mue.Common.Models
{
    public record GenericResponse
    {
        public bool Success { get; init; }
    }

    public record ErrorResponse
    {
        public string Message { get; init; }
        public MueCodes Code { get; init; }
    }

    public record AuthRequest
    {
        public string Username { get; init; }
        public string Password { get; init; }
        public bool IsRegistration { get; init; }
    }

    public record CommandRequest
    {
        public bool IsExpanded { get; init; } // If true, only use Command, else Command and Params are split (TODO: Make this smarter?)
        public string Command { get; init; }
        public IDictionary<string, string> Params { get; init; }
    }

    public record CommunicationsMessage
    {
        public const string META_ERRTYPE = "error_type";
        public const string META_RENDERER = "message_renderer";
        public const string META_RENDERER_DEFAULT = "plaintext";
        public const string META_RENDERER_TABLE = "table";
        public const string META_RENDERER_CLIENTONLY = "client_only";
        public const string META_TABLE_CONTENT = "table_content";

        public string Source { get; init; }
        public string Target { get; init; }
        public string Message { get; init; }
        public IReadOnlyDictionary<string, string> ExtendedContent { get; init; }
        public string ExtendedFormat { get; init; }
        public IDictionary<string, string> Meta { get; init; }
    }

    public record CommunicationsMessage_Table
    {
        public string Message { get; init; }
        public IEnumerable<IEnumerable<string>> Table { get; init; }
        public bool HasHeader { get; init; }
    }
}