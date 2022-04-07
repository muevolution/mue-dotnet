using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Mue.Server.Core.Utils;

public static class Json
{
    public static readonly JsonSerializerSettings JsonConfig = UpdateJsonConfig(new JsonSerializerSettings());

    public static JsonSerializerSettings UpdateJsonConfig(JsonSerializerSettings settings)
    {
        settings.ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        };
        settings.Formatting = Formatting.None;
        settings.NullValueHandling = NullValueHandling.Ignore;

        return settings;
    }

    public static string Serialize<T>(T? obj)
    {
        return JsonConvert.SerializeObject(obj, JsonConfig);
    }

    public static T? Deserialize<T>(string json)
    {
        if (String.IsNullOrEmpty(json))
        {
            return default(T);
        }

        return JsonConvert.DeserializeObject<T>(json, JsonConfig);
    }

    public static IDictionary<string, string> ToFlatDictionary<T>(T obj)
    {
        return Deserialize<Dictionary<string, string>>(Serialize(obj))!;
    }

    public static T FromFlatDictionary<T>(IEnumerable<KeyValuePair<string, string>> dict)
    {
        return Deserialize<T>(Serialize(dict))!;
    }
}
