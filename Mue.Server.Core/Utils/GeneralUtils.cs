namespace Mue.Server.Core.Utils;

public static class GeneralUtils
{
    public static string GenerateRandomId()
    {
        // TODO: Not this
        return Guid.NewGuid().ToString().Replace("-", String.Empty).Substring(0, 8);
    }

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> value)
    {
        return value.Where(w => w != null).Cast<T>();
    }

    public static IDictionary<T1, T2> WhereNotNull<T1, T2>(this IEnumerable<KeyValuePair<T1, T2?>> value) where T1 : notnull
    {
        return value.Where(w => w.Value != null).Cast<KeyValuePair<T1, T2>>().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public static IDictionary<string, string>? MergeDicts(IEnumerable<KeyValuePair<string, string>>? dictA, IEnumerable<KeyValuePair<string, string>>? dictB)
    {
        if (dictA == null && dictB == null)
        {
            return null;
        }

        var outputDict = new Dictionary<string, string>();

        if (dictA != null)
        {
            foreach (var kvp in dictA)
            {
                outputDict.TryAdd(kvp.Key, kvp.Value);
            }
        }
        if (dictB != null)
        {
            foreach (var kvp in dictB)
            {
                outputDict.TryAdd(kvp.Key, kvp.Value);
            }
        }

        return outputDict;
    }
}
