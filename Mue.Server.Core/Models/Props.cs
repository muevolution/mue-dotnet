using System;
using System.Collections.Generic;
using System.Linq;
using Mue.Server.Core.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mue.Server.Core.Models
{
    public enum PropValueType
    {
        Unset,
        String,
        Number,
        List,
    }

    public record PropValue
    {
        public PropValue() { }

        public PropValue(string val)
        {
            ValueType = PropValueType.String;
            StringValue = val;
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

        public PropValueType ValueType { get; init; }
        public string StringValue { get; init; }
        public int? NumberValue { get; init; }
        public IEnumerable<FlatPropValue> ListValue { get; init; }

        public bool IsNull { get { return ValueType == PropValueType.Unset; } }

        public override string ToString()
        {
            return ValueType switch
            {
                PropValueType.String => StringValue,
                PropValueType.Number => NumberValue.Value.ToString(),
                PropValueType.List => "[" + String.Join(",", ListValue.Select(s => s.ValueType switch
                {
                    FlatPropValueType.String => s.StringValue as object,
                    FlatPropValueType.Number => s.NumberValue as object,
                    _ => null,
                })) + "]",
                _ => "unknown",
            };
        }

        public dynamic ToDynamic()
        {
            return ValueType switch
            {
                PropValueType.String => StringValue,
                PropValueType.Number => NumberValue.Value,
                PropValueType.List => ListValue.Select(s => s.ValueType switch
                {
                    FlatPropValueType.String => s.StringValue as object,
                    FlatPropValueType.Number => s.NumberValue as object,
                    _ => null,
                }).ToArray(),
                _ => null,
            };
        }

        public string ToJsonString()
        {
            if (IsNull)
            {
                return null;
            }

            return Json.Serialize(ToDynamic());
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
                JTokenType.None or JTokenType.Null => new PropValue { ValueType = PropValueType.Unset },
                JTokenType.String => new PropValue { ValueType = PropValueType.String, StringValue = (string)jobj },
                JTokenType.Integer => new PropValue { ValueType = PropValueType.Number, NumberValue = (int)jobj },
                JTokenType.Array => new PropValue
                {
                    ValueType = PropValueType.List,
                    ListValue = ((JArray)jobj).Select(s => s.Type switch
                    {
                        JTokenType.String => new FlatPropValue((string)s),
                        JTokenType.Integer => new FlatPropValue((int)s),
                        _ => throw new Exception($"Failed to decode list PropValue from JSON (unsupported type {s.Type}"),
                    }).ToArray()
                },
                _ => throw new Exception($"Failed to decode PropValue from JSON (unsupported root type {jobj.Type})"),
            };
        }
    }

    public enum FlatPropValueType
    {
        String,
        Number,
    }

    public record FlatPropValue
    {
        public FlatPropValue() { }

        public FlatPropValue(string val)
        {
            ValueType = FlatPropValueType.String;
            StringValue = val;
        }

        public FlatPropValue(int val)
        {
            ValueType = FlatPropValueType.Number;
            NumberValue = val;
        }

        public FlatPropValueType ValueType { get; init; }
        public string StringValue { get; init; }
        public int? NumberValue { get; init; }
    }
}