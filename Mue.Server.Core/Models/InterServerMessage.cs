using System;
using System.Collections.Generic;

namespace Mue.Server.Core.Models
{
    public interface IGlobalUpdate
    {
        string EventName { get; }
    }

    /// <summary>ISM message is serialized to JSON so it needs weaker typing</summary>
    public record InterServerMessage : IGlobalUpdate
    {
        public const string EVENT_JOINED = "joined";
        public const string EVENT_INVALIDATE_SCRIPT = "invalidate_script";
        public const string EVENT_UPDATE_OBJECT = "update_object";

        public string InstanceId { get; init; }
        public string EventName { get; init; }
        public IDictionary<string, string> Meta { get; init; }

        public static InterServerMessage CreateJoinedMessage(string instanceName)
        {
            return new InterServerMessage
            {
                InstanceId = instanceName,
                EventName = EVENT_JOINED,
            };
        }

        public static InterServerMessage CreateInvalidateScript(string instanceName)
        {
            return new InterServerMessage
            {
                InstanceId = instanceName,
                EventName = EVENT_INVALIDATE_SCRIPT,
            };
        }

        public static InterServerMessage CreateObjectUpdate(string instanceName, string objectId, string updateType)
        {
            return new InterServerMessage
            {
                InstanceId = instanceName,
                EventName = EVENT_UPDATE_OBJECT,
                Meta = new Dictionary<string, string> {
                    {"id", objectId},
                    {"message", updateType},
                },
            };
        }
    }

    public record ObjectUpdate : IGlobalUpdate
    {
        public const string EVENT_MOVE = "move";
        public const string EVENT_REPARENT = "reparent";
        public const string EVENT_RENAME = "rename";
        public const string EVENT_INVALIDATE = "invalidate";
        public const string EVENT_DESTROY = "destroy";

        public ObjectId Id { get; init; }
        public string EventName { get; init; }
        public IObjectUpdateResult Meta { get; init; }
    }

    public record PlayerUpdate : ObjectUpdate
    {
        public const string EVENT_QUIT = "quit";
    }

    public interface IObjectUpdateResult { }

    public record RenameResult : IObjectUpdateResult
    {
        public string OldName { get; init; }
        public string NewName { get; init; }
    }

    public record ReparentResult : IObjectUpdateResult
    {
        public ObjectId OldParent { get; init; }
        public ObjectId NewParent { get; init; }
    }

    public record MoveResult : IObjectUpdateResult
    {
        public ObjectId OldLocation { get; init; }
        public ObjectId NewLocation { get; init; }
    }

    public interface IPlayerUpdateResult : IObjectUpdateResult { }

    public record QuitResult : IPlayerUpdateResult
    {
        public string Reason { get; init; }
    }
}