namespace Mue.Server.Core.Objects;

public record ObjectMetadata
{
    public string Name { get; init; } = null!;
    public ObjectId Creator { get; init; } = null!;
    public ObjectId Parent { get; init; } = null!;
    public ObjectId? Location { get; init; } = null!;

    public IDictionary<string, string> ToDictionary()
    {
        return Json.ToFlatDictionary(this);
    }

    public static T FromDictionary<T>(IEnumerable<KeyValuePair<string, string>> dict) where T : ObjectMetadata
    {
        return Json.FromFlatDictionary<T>(dict);
    }
}
