using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Mue.Server.Core.Models;
using Mue.Server.Core.System;
using Mue.Server.Core.Utils;

namespace Mue.Server.Core.Objects
{
    public abstract class GameObject<MD> : IGameObject<MD> where MD : ObjectMetadata
    {
        protected IWorld _world;

        public GameObjectType ObjectType { get; protected set; }
        public ObjectId Id { get; protected set; }
        public bool IsPendingAdd => !Id.IsAssigned;
        public bool IsDestroyed { get; protected set; }
        public MD Meta { get; protected set; }
        public ObjectMetadata MetaBasic => Meta;
        public IReadOnlyDictionary<string, string> MetaDictionary => Meta.ToDictionary();

        protected GameObject(IWorld world, GameObjectType objType, MD meta, ObjectId id = null)
        {
            _world = world;
            ObjectType = objType;
            Meta = meta;
            Id = id ?? ObjectId.Empty;
        }

        public void SetInitialId(string newId)
        {
            if (!this.IsPendingAdd)
            {
                // TODO: Give specific error
                throw new Exception("Cannot reassign an ID to an object with one already defined");
            }

            if (String.IsNullOrEmpty(newId))
            {
                // TODO: Give specific error
                throw new Exception("Reassigned ID cannot be null");
            }

            this.Id = new ObjectId(newId, ObjectType);
        }

        public string Name => Meta.Name;
        public ObjectId Parent => Meta.Parent;
        public bool IsParentRoot => Meta.Parent == Id;
        public ObjectId Location => Meta.Location;
        public bool IsLocationRoot => Meta.Location == Id;

        public Task<PropValue> GetProp(string path)
        {
            return _world.StorageManager.GetProp(this.Id, path);
        }

        public Task<IReadOnlyDictionary<string, PropValue>> GetProps()
        {
            return _world.StorageManager.GetProps(this.Id);
        }

        public Task<bool> SetProp(string path, PropValue value)
        {
            return _world.StorageManager.SetProp(this.Id, path, value);
        }

        public Task<bool> SetProps(IReadOnlyDictionary<string, PropValue> values)
        {
            return _world.StorageManager.SetProps(this.Id, values);
        }

        public bool MatchName(string term)
        {
            if (String.IsNullOrEmpty(term))
            {
                return false;
            }

            // TODO: Add fuzzy matching
            return term.Trim().ToLower() == this.Meta.Name.ToLower();
        }

        public async Task<bool> Rename(string newName)
        {
            newName = newName?.Trim();

            if (String.IsNullOrWhiteSpace(newName) || Name == newName)
            {
                return false;
            }

            var currentMeta = await _world.StorageManager.GetMeta<MD>(this.Id);
            var newMeta = currentMeta with { Name = newName };
            var updatedMeta = await _world.StorageManager.UpdateMeta(this.Id, newMeta);
            var updatedIndex = true;

            if (this.Id.ObjectType == GameObjectType.Player)
            {
                updatedIndex = await _world.StorageManager.UpdatePlayerNameIndex(this.Id, currentMeta.Name, newName);
            }

            var output = new RenameResult { OldName = currentMeta.Name, NewName = newName };
            FireObjectEvent(ObjectUpdate.EVENT_RENAME, output);

            return updatedMeta && updatedIndex;
        }

        public virtual Task<ReparentResult> Reparent(ObjectId newParent)
        {
            return Reparent(newParent, GameObjectConsts.AllContainerTypes);
        }

        protected async Task<ReparentResult> Reparent(ObjectId newParent, IEnumerable<GameObjectType> restrictedTypes)
        {
            if (newParent == null)
            {
                // Don't allow reparenting to nothing
                return null;
            }

            // Check requested type restrictions
            if (restrictedTypes != null)
            {
                if (!restrictedTypes.Contains(newParent.ObjectType))
                {
                    throw new InvalidGameObjectParentException(newParent);
                }
            }

            // Not allowed to be outside of an allowed parent type no matter what
            if (!GameObjectConsts.AllParentTypes.Contains(newParent.ObjectType))
            {
                throw new InvalidGameObjectParentException(newParent);
            }

            // Can't move into a destroyed location
            var newParentExists = await _world.StorageManager.DoesObjectExist(newParent);
            if (!newParentExists)
            {
                throw new GameObjectDestroyedException(newParent);
            }

            // Handle the reparent (using live data)
            var oldParentIdStr = await _world.StorageManager.GetMeta(Id, "parent");
            var oldParent = oldParentIdStr != null ? new ObjectId(oldParentIdStr) : null;
            var result = await _world.StorageManager.ReparentObject(Id, newParent, oldParent);
            if (!result)
            {
                return null;
            }

            Meta = Meta with { Parent = newParent };

            var output = new ReparentResult { OldParent = oldParent, NewParent = newParent };
            FireObjectEvent(ObjectUpdate.EVENT_REPARENT, output);
            return output;
        }

        public virtual Task<MoveResult> Move(ObjectId newLocation)
        {
            return Move(newLocation, GameObjectConsts.AllContainerTypes);
        }

        protected async Task<MoveResult> Move(ObjectId newLocation, IEnumerable<GameObjectType> restrictedTypes)
        {
            if (newLocation == null)
            {
                // Don't allow moving into nothing
                return null;
            }

            // Check requested type restrictions
            if (restrictedTypes != null)
            {
                if (!restrictedTypes.Contains(newLocation.ObjectType))
                {
                    throw new InvalidGameObjectLocationException(newLocation);
                }
            }

            // Not allowed to be outside of an allowed container type no matter what
            if (!GameObjectConsts.AllContainerTypes.Contains(newLocation.ObjectType))
            {
                throw new InvalidGameObjectLocationException(newLocation);
            }

            // Can't move into a destroyed location
            var newLocationExists = await _world.StorageManager.DoesObjectExist(newLocation);
            if (!newLocationExists)
            {
                throw new GameObjectDestroyedException(newLocation);
            }

            // Handle the move (using live data)
            var oldLocationIdStr = await _world.StorageManager.GetMeta(Id, "location");
            var oldLocation = oldLocationIdStr != null ? new ObjectId(oldLocationIdStr) : null;
            var result = await _world.StorageManager.MoveObject(Id, newLocation, oldLocation);
            if (!result)
            {
                return null;
            }

            return MoveFinish(newLocation, oldLocation);
        }

        public MoveResult MoveFinish(ObjectId newLocation, ObjectId oldLocation = null)
        {
            Meta = Meta with { Location = newLocation };

            var output = new MoveResult { OldLocation = oldLocation, NewLocation = newLocation };
            FireObjectEvent(ObjectUpdate.EVENT_MOVE, output);
            return output;
        }

        public virtual async Task<bool> Destroy()
        {
            await _world.StorageManager.DestroyObject(this);
            this.IsDestroyed = true;
            await _world.ObjectCache.OnDestroy(this);
            return true;
        }

        public virtual async Task<bool> Invalidate()
        {
            var newMeta = await _world.StorageManager.GetMeta<MD>(Id);
            if (newMeta == null)
            {
                // TODO: Throw more specific error?
                throw new GameObjectIdDoesNotExistException(Id);
            }

            this.Meta = newMeta;
            return true;
        }

        public override string ToString()
        {
            return $"'{Name}' [{Id}]";
        }

        protected void FireObjectEvent(string eventName, IObjectUpdateResult meta = null)
        {
            this._world.FireObjectEvent(this.Id, eventName, meta);
        }
    }

    // Default inheritance type is the base ObjectMetadata
    public abstract class GameObject : GameObject<ObjectMetadata>
    {
        protected GameObject(IWorld world, GameObjectType objType, ObjectMetadata meta, ObjectId id = null) : base(world, objType, meta, id) { }
    }
}