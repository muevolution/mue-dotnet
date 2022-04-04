using System;
using System.Collections.Generic;
using Mue.Server.Core.Models;
using Mue.Server.Core.Utils;

namespace Mue.Server.Core.Objects
{
    public record ObjectMetadata
    {
        public string Name { get; init; } = null!;
        public ObjectId Creator { get; init; } = null!;
        public ObjectId Parent { get; init; } = null!;
        public ObjectId? Location { get; init; } = null!;

        public IReadOnlyDictionary<string, string> ToDictionary()
        {
            return Json.ToFlatDictionary(this);
        }

        public static T FromDictionary<T>(IReadOnlyDictionary<string, string> dict) where T : ObjectMetadata
        {
            return Json.FromFlatDictionary<T>(dict);
        }
    }
}
