using System;
using Mue.Server.Core.Objects;
using Mue.Server.Core.Utils;
using Newtonsoft.Json;

namespace Mue.Server.Core.Models
{
    [JsonConverter(typeof(ObjectIdConverter))]
    public record ObjectId
    {
        public static readonly ObjectId Empty = new ObjectId();

        public ObjectId()
        {
            IsAssigned = false;
        }

        public ObjectId(string anyId, GameObjectType? checkType = null)
        {
            if (String.IsNullOrWhiteSpace(anyId))
            {
                throw new IllegalObjectIdConstructorException("Object ID cannot be empty.", anyId);
            }

            var a = anyId.Split(":");
            if (a.Length < 2)
            {
                // Only given a shortid
                ShortId = a[0];

                if (checkType.HasValue)
                {
                    // We were given a checkType which means we can use it as the origin type
                    // This makes the shortid valid
                    ObjectType = checkType.Value;
                    IsAssigned = true;
                }

                return;
            }

            // We should have a full ID here (T:qwerty)

            if (String.IsNullOrWhiteSpace(a[0]))
            {
                throw new IllegalObjectIdConstructorException("Type cannot be empty.", anyId);
            }
            else if (String.IsNullOrWhiteSpace(a[1]))
            {
                throw new IllegalObjectIdConstructorException("ID cannot be empty.", anyId);
            }

            ObjectType = GameObjectConsts.GetGameObjectType(a[0]);

            if (ObjectType == GameObjectType.Invalid)
            {
                throw new IllegalObjectIdConstructorException($"Invalid object type {a[0]}", anyId);
            }
            else if (checkType.HasValue && ObjectType != checkType.Value)
            {
                throw new IllegalObjectIdConstructorException($"Object ID {anyId} does not match requested type {checkType}", anyId);
            }

            ShortId = a[1];
            IsAssigned = true;
        }

        public bool IsAssigned { get; init; }
        public string Id { get { return ShortId != null && ObjectType != GameObjectType.Invalid ? $"{ObjectType.ToShortString()}:{ShortId}" : null; } }
        public GameObjectType ObjectType { get; init; } = GameObjectType.Invalid;
        public string ShortId { get; init; }

        public override string ToString()
        {
            return Id;
        }
    }

    class ObjectIdConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var objId = value as ObjectId;

            if (!objId.IsAssigned)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(objId.Id);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var id = (string)reader.Value;
            if (id == null)
            {
                return ObjectId.Empty;
            }
            else
            {
                return new ObjectId(id);
            }
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(ObjectId);
        }
    }
}
