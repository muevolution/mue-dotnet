using System;
using System.Collections.Generic;
using Mue.Server.Core.Utils;

namespace Mue.Server.Core.Models
{
    public interface IGlobalUpdate
    {
        DateTime EventTime { get; }
        string EventName { get; }
    }

    /// <summary>ISM message is serialized to JSON so it needs weaker typing</summary>
    public record InterServerMessage(
        string InstanceId,
        string EventName,
        IDictionary<string, string>? Meta = null
    ) : IGlobalUpdate
    {
        public const string EVENT_JOINED = "joined";
        public const string EVENT_INVALIDATE_SCRIPT = "invalidate_script";
        public const string EVENT_UPDATE_OBJECT = "update_object";
        public const string EVENT_UPDATE_PLAYER = "update_player";

        public DateTime EventTime { get; init; } = TimeUtil.MueNow;

        public static InterServerMessage CreateJoinedMessage(string instanceName)
        {
            return new InterServerMessage(instanceName, EVENT_JOINED);
        }

        public static InterServerMessage CreateInvalidateScript(string instanceName)
        {
            return new InterServerMessage(instanceName, EVENT_INVALIDATE_SCRIPT);
        }

        public static InterServerMessage CreateObjectUpdate(string instanceName, string objectId, string updateType)
        {
            return new InterServerMessage(instanceName, EVENT_UPDATE_OBJECT)
            {
                Meta = new Dictionary<string, string> {
                    {"id", objectId},
                    {"message", updateType},
                },
            };
        }

        public static InterServerMessage CreatePlayerUpdate(string instanceName, string objectId, string updateType)
        {
            return new InterServerMessage(instanceName, EVENT_UPDATE_PLAYER)
            {
                Meta = new Dictionary<string, string> {
                    {"id", objectId},
                    {"message", updateType},
                },
            };
        }
    }

    public record ObjectUpdate(
        ObjectId Id,
        string EventName,
        IObjectUpdateResult? Meta = null
    ) : IGlobalUpdate
    {
        public const string EVENT_MOVE = "move";
        public const string EVENT_REPARENT = "reparent";
        public const string EVENT_RENAME = "rename";
        public const string EVENT_INVALIDATE = "invalidate";
        public const string EVENT_DESTROY = "destroy";

        public DateTime EventTime { get; init; } = TimeUtil.MueNow;
    }

    public record PlayerUpdate(
        ObjectId Id,
        string EventName,
        IObjectUpdateResult? Meta = null
    ) : ObjectUpdate(Id, EventName, Meta)
    {
        public const string EVENT_CONNECT = "connect";
        public const string EVENT_DISCONNECT = "disconnect";
        public const string EVENT_QUIT = "quit";
    }

    public interface IObjectUpdateResult { }

    public record EmptyObjectUpdateResult : IObjectUpdateResult
    {
        public static EmptyObjectUpdateResult Empty = new EmptyObjectUpdateResult();
    }

    public record RenameResult(string? OldName, string NewName) : IObjectUpdateResult;

    public record ReparentResult(ObjectId? OldParent, ObjectId NewParent) : IObjectUpdateResult;

    public record MoveResult(ObjectId? OldLocation, ObjectId NewLocation) : IObjectUpdateResult;

    public interface IPlayerUpdateResult : IObjectUpdateResult { }

    public record PlayerConnectionResult : IPlayerUpdateResult
    {
        // TODO: Implement actually using this
        public int RemainingConnections { get; init; }
    }

    public record QuitResult(string? Reason) : IPlayerUpdateResult;
}