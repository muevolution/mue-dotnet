using Newtonsoft.Json.Linq;

namespace Mue.Server.Core.Models;

public enum PropValueType
{
    Unset,
    ObjectId,
    String,
    Number,
    List,
}

public class PropValue
{
    internal const string ObjectIdPrefix = "_oid:";
    public static readonly PropValue EmptyList = new PropValue(new FlatPropValue[] { });

    public PropValue()
    {
        ValueType = PropValueType.Unset;
    }

    public PropValue(ObjectId val)
    {
        ValueType = PropValueType.ObjectId;
        ObjectIdValue = val;
    }

    public PropValue(string? val)
    {
        if (val == null)
        {
            ValueType = PropValueType.Unset;
        }
        else
        {
            ValueType = PropValueType.String;
            StringValue = val;
        }
    }

    public PropValue(int val)
    {
        ValueType = PropValueType.Number;
        NumberValue = val;
    }

    public PropValue(IEnumerable<FlatPropValue> val)
    {
        ValueType = PropValueType.List;
        ListValue = val;
    }

    private PropValue(JArray jarr)
    {
        ValueType = PropValueType.List;
        ListValue = jarr.Select(s => FlatPropValue.FromJson(s)).WhereNotNull().ToList();
    }

    public PropValueType ValueType { get; private init; }
    public ObjectId? ObjectIdValue { get; private init; }
    public string? StringValue { get; private init; }
    public int? NumberValue { get; private init; }
    public IEnumerable<FlatPropValue> ListValue { get; private init; } = Enumerable.Empty<FlatPropValue>();

    public bool IsNull { get { return ValueType == PropValueType.Unset; } }

    public override bool Equals(object? obj)
    {
        if (this == obj)
        {
            return true;
        }
        else if (obj == null || !this.GetType().Equals(obj.GetType()))
        {
            return false;
        }

        var cObj = ((PropValue)obj);
        if (this.ValueType != cObj.ValueType)
        {
            return false;
        }

        if (this.ValueType == PropValueType.Unset)
        {
            return true;
        }
        else if (this.ValueType == PropValueType.List)
        {
            return this.ListValue.SequenceEqual(cObj.ListValue);
        }
        else
        {
            return this.ToDynamic() == cObj.ToDynamic();
        }
    }

    public override int GetHashCode()
    {
        return this.ToDynamic()?.GetHashCode();
    }

    public override string? ToString()
    {
        return ValueType switch
        {
            PropValueType.ObjectId => ObjectIdValue?.Id,
            PropValueType.String => StringValue,
            PropValueType.Number => NumberValue?.ToString(),
            PropValueType.List => "[" + String.Join(",", ListValue.Select(s => s.ToString())) + "]",
            _ => "unknown",
        };
    }

    public dynamic? ToDynamic(bool forJson = false)
    {
        return ValueType switch
        {
            PropValueType.ObjectId => forJson ? $"{ObjectIdPrefix}{ObjectIdValue}" : ObjectIdValue,
            PropValueType.String => StringValue,
            PropValueType.Number => NumberValue,
            PropValueType.List => ListValue?.Select(s => s.ToDynamic(forJson)).ToArray(),
            _ => null,
        };
    }

    public string? ToJsonString()
    {
        if (IsNull)
        {
            return null;
        }

        return Json.Serialize(ToDynamic(true));
    }

    public static PropValue FromJsonString(string json)
    {
        if (json == null)
        {
            return new PropValue();
        }

        var jobj = JValue.Parse(json);
        return FromJson(jobj);
    }

    private static PropValue FromJson(JToken jobj)
    {
        return jobj.Type switch
        {
            JTokenType.None or JTokenType.Null => new PropValue(),
            JTokenType.String => PropValue.FromJsonStringDeterminer(jobj),
            JTokenType.Integer => new PropValue((int)jobj),
            JTokenType.Array => new PropValue((JArray)jobj),
            _ => throw new Exception($"Failed to decode PropValue from JSON (unsupported root type {jobj.Type})"),
        };
    }

    public static PropValue FromJsonStringDeterminer(JToken jobj)
    {
        var str = (string?)jobj;

        if (str?.StartsWith(ObjectIdPrefix) ?? false)
        {
            var objStr = str.Substring(ObjectIdPrefix.Length);
            try
            {
                var id = new ObjectId(objStr);
                if (id.IsAssigned && id.ObjectType != Objects.GameObjectType.Invalid)
                {
                    return new PropValue(id);
                }
            }
            catch (IllegalObjectIdConstructorException)
            {
                // Not an ObjectId
            }
        }

        return new PropValue(str);
    }
}

public enum FlatPropValueType
{
    Unset,
    ObjectId,
    String,
    Number,
}

public class FlatPropValue
{
    public FlatPropValue()
    {
        ValueType = FlatPropValueType.Unset;
    }

    public FlatPropValue(ObjectId val)
    {
        ValueType = FlatPropValueType.ObjectId;
        ObjectIdValue = val;
    }

    public FlatPropValue(string? val)
    {
        if (val != null)
        {
            ValueType = FlatPropValueType.String;
            StringValue = val;
        }
    }

    public FlatPropValue(int val)
    {
        ValueType = FlatPropValueType.Number;
        NumberValue = val;
    }

    public FlatPropValueType ValueType { get; private init; }
    public ObjectId? ObjectIdValue { get; private init; }
    public string? StringValue { get; private init; }
    public int? NumberValue { get; private init; }

    public override bool Equals(object? obj)
    {
        if (this == obj)
        {
            return true;
        }
        else if (obj == null || !this.GetType().Equals(obj.GetType()))
        {
            return false;
        }

        var cObj = ((FlatPropValue)obj);
        if (this.ValueType != cObj.ValueType)
        {
            return false;
        }

        return this.ToDynamic() == cObj.ToDynamic();
    }

    public override int GetHashCode()
    {
        return this.ToDynamic()?.GetHashCode();
    }

    public override string? ToString()
    {
        return ValueType switch
        {
            FlatPropValueType.ObjectId => ObjectIdValue?.Id,
            FlatPropValueType.String => StringValue,
            FlatPropValueType.Number => NumberValue?.ToString(),
            _ => "unknown",
        };
    }

    internal dynamic? ToDynamic(bool forJson = false)
    {
        return ValueType switch
        {
            FlatPropValueType.ObjectId => forJson ? $"{PropValue.ObjectIdPrefix}{ObjectIdValue}" : ObjectIdValue,
            FlatPropValueType.String => StringValue,
            FlatPropValueType.Number => NumberValue,
            _ => null,
        };
    }

    internal static FlatPropValue? FromJson(JToken jobj)
    {
        return jobj.Type switch
        {
            JTokenType.None or JTokenType.Null => null,
            JTokenType.String => FromJsonStringDeterminer(jobj),
            JTokenType.Integer => new FlatPropValue((int)jobj),
            _ => throw new Exception($"Failed to decode FlatPropValue from JSON (unsupported root type {jobj.Type})"),
        };
    }

    private static FlatPropValue? FromJsonStringDeterminer(JToken jobj)
    {
        var str = (string?)jobj;

        if (str?.StartsWith(PropValue.ObjectIdPrefix) ?? false)
        {
            try
            {
                var objStr = str.Substring(PropValue.ObjectIdPrefix.Length);
                var id = new ObjectId(objStr);
                if (id.IsAssigned && id.ObjectType != Objects.GameObjectType.Invalid)
                {
                    return new FlatPropValue(id);
                }
            }
            catch (IllegalObjectIdConstructorException)
            {
                // Not an ObjectId
            }
        }

        return new FlatPropValue(str);
    }
}
