using System;
using System.Collections.Generic;
using System.Linq;

namespace Mue.Server.Core.Objects
{
    public enum GameObjectType
    {
        Invalid,
        Room,
        Player,
        Item,
        Script,
        Action,
    }

    public static class GameObjectConsts
    {
        private static readonly IReadOnlyDictionary<GameObjectType, string> GameObjectTypes = new Dictionary<GameObjectType, string> {
            {GameObjectType.Room, "r"},
            {GameObjectType.Player, "p"},
            {GameObjectType.Item, "i"},
            {GameObjectType.Script, "s"},
            {GameObjectType.Action, "a"},
        };

        private static readonly IReadOnlyDictionary<Type, GameObjectType> GameObjectTypeTypes = new Dictionary<Type, GameObjectType> {
            {typeof(GameRoom), GameObjectType.Room},
            {typeof(GamePlayer), GameObjectType.Player},
            {typeof(GameItem), GameObjectType.Item},
            {typeof(GameScript), GameObjectType.Script},
            {typeof(GameAction), GameObjectType.Action},
        };

        private static readonly IReadOnlyDictionary<string, GameObjectType> GameObjectTypeStrings = GameObjectTypes.ToDictionary(k => k.Value, k => k.Key);
        public static readonly IEnumerable<string> GameObjectValidTypePrefixes = GameObjectTypes.Values;

        public static string ToShortString(this GameObjectType type)
        {
            if (GameObjectTypes.ContainsKey(type))
            {
                return GameObjectTypes[type];
            }
            else
            {
                return "?";
            }
        }

        public static GameObjectType GetGameObjectType(string type)
        {
            return GameObjectTypeStrings[type];
        }

        public static GameObjectType GetGameObjectType<T>() where T : IGameObject
        {
            return GameObjectTypeTypes[typeof(T)];
        }

        public static readonly GameObjectType[] AllGameObjectTypes = {
            GameObjectType.Room,
            GameObjectType.Player,
            GameObjectType.Item,
            GameObjectType.Script,
            GameObjectType.Action,
        };

        public static readonly GameObjectType[] AllContainerTypes = {
            GameObjectType.Room,
            GameObjectType.Player,
            GameObjectType.Item,
        };

        public static readonly GameObjectType[] AllParentTypes = AllContainerTypes;

        public static readonly GameObjectType[] AllActionTargets = {
            GameObjectType.Room,
            GameObjectType.Script,
        };
    }

    public static class GameObjectExtensions
    {
        /// <summary>Safely filter an enumerable of game objects and cast them.</summary>
        public static IEnumerable<T> WhereObjectType<T>(this IEnumerable<IGameObject> items) where T : IGameObject
        {
            return items.Where(w => w.ObjectType == GameObjectConsts.GetGameObjectType<T>()).Cast<T>();
        }
    }

    public enum RootField
    {
        Unused,
        RootRoom,
        StartRoom,
        PlayerRoot,
        God,
    }

    public static class RootFieldConsts
    {
        private static readonly IReadOnlyDictionary<RootField, string> RootFieldStrings = new Dictionary<RootField, string> {
            {RootField.RootRoom, "root_room"},
            {RootField.StartRoom, "start_room"},
            {RootField.PlayerRoot, "player_root"},
            {RootField.God, "god"},
        };

        private static readonly IReadOnlyDictionary<string, RootField> RootFieldByString = RootFieldStrings.ToDictionary(k => k.Value, k => k.Key);

        public static string GetRootFieldString(RootField type)
        {
            return RootFieldStrings[type];
        }

        public static RootField GetRootField(string type)
        {
            return RootFieldByString[type];
        }
    }
}
