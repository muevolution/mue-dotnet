using System;
using System.Collections.Generic;

namespace Mue.Common.Models
{
    public record LocalCommand
    {
        public string Command { get; init; }
        public string Args { get; init; }
        public IDictionary<string, string> Params { get; init; }

        public bool IsBare => String.IsNullOrEmpty(Args) && (Params == null || Params.Count < 1);
    }

    public struct MessageFormats
    {
        public string FirstPerson;
        public string ThirdPerson;
    }

    public record InteriorMessage : CommunicationsMessage
    {
        public new MessageFormats? ExtendedFormat;
        public string Script { get; init; }
    }
}
