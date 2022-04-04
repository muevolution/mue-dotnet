using System;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;

namespace Mue.Server.Core.Utils
{
    public abstract class GameException : Exception
    {
        protected dynamic? _metadata;

        public GameException() : base() { }

        public GameException(string message, dynamic? metadata = null) : base(message)
        {
            _metadata = metadata;
        }
    }

    public class UnusableWorldException : GameException
    {
        public UnusableWorldException(string message) : base($"The server is not in a usable state. {message}") { }
    }

    public class WorldShutdownError : GameException
    {
        public WorldShutdownError() : base("The world has been shut down.") { }
    }


    public class WorldNotInitException : GameException
    {
        public WorldNotInitException() : base("The world has not yet been initialized.") { }
    }

    public abstract class GameObjectException : GameException
    {
        public GameObjectException(ObjectId objectId, string message) : base($"Object({objectId.ObjectType}) with the ID {objectId} {message}", new { objectId }) { }
    }

    public class GameObjectIdExistsException : GameObjectException
    {
        public GameObjectIdExistsException(ObjectId objectId) : base(objectId, "already exists!") { }
    }

    public class GameObjectIdDoesNotExistException : GameObjectException
    {
        public GameObjectIdDoesNotExistException(ObjectId objectId) : base(objectId, "does not exist") { }
    }

    public class GameObjectTypeDoesNotMatchException : GameObjectException
    {
        public GameObjectTypeDoesNotMatchException(ObjectId objectId, GameObjectType expected) : base(objectId, $"does not match {expected}") { }
    }

    public class GameObjectDestroyedException : GameObjectException
    {
        public GameObjectDestroyedException(ObjectId objectId) : base(objectId, "has been destroyed") { }
    }

    public class InvalidGameObjectStateException : GameObjectException
    {
        public InvalidGameObjectStateException(ObjectId objectId) : base(objectId, "is in an invalid state") { }
    }

    public class InvalidGameObjectNameException : GameObjectException
    {
        public InvalidGameObjectNameException(ObjectId objectId) : base(objectId, "does not have a proper name") { }
    }

    public class InvalidGameObjectParentException : GameObjectException
    {
        public InvalidGameObjectParentException(ObjectId objectId) : base(objectId, "is not a valid parent") { }
    }

    public class InvalidGameObjectLocationException : GameObjectException
    {
        public InvalidGameObjectLocationException(ObjectId objectId) : base(objectId, "is not a valid location") { }
    }

    public class InvalidGameObjectTargetException : GameObjectException
    {
        public InvalidGameObjectTargetException(ObjectId objectId) : base(objectId, "is not a valid target") { }
    }

    public class PlayerNameAlreadyExistsException : GameException
    {
        public PlayerNameAlreadyExistsException(string playerName, ObjectId existingPlayer) : base($"Player with the name {playerName} already exists [{existingPlayer}]", new { playerName, existingPlayer }) { }
    }

    public class IllegalObjectNameException : GameException
    {
        public IllegalObjectNameException(string name, GameObjectType type) : base($"Object({type}) was given an illegal name \"{name}\"", new { name, type }) { }
    }

    public class IllegalObjectIdException : GameException
    {
        public IllegalObjectIdException(string id) : base($"Object was given an illegal id \"{id}\"", new { id }) { }
        public IllegalObjectIdException(ObjectId id) : base($"Object was given an illegal id \"{id}\"", new { id }) { }
    }

    public class IllegalObjectIdConstructorException : GameException
    {
        public IllegalObjectIdConstructorException(string message, string? id) : base(message, new { id = id }) { }
    }
}