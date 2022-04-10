using Mue.Backend.Storage;

namespace Mue.Server.Core.System;

public static class StorageManagerKeys
{
    public static string GetKeyStructure(ObjectId owner, string key) => $"s:{owner.Id}:{key}";
    public static string GetPropKeyStructure(ObjectId owner) => StorageManagerKeys.GetKeyStructure(owner, "props");
    public static string GetContentsKeyStructure(ObjectId owner) => StorageManagerKeys.GetKeyStructure(owner, "contents");
    public static string GetMetaKeyStructure(ObjectId owner) => StorageManagerKeys.GetKeyStructure(owner, "meta");
    public static string GetScriptKeyStructure(ObjectId owner) => StorageManagerKeys.GetKeyStructure(owner, "script");
    public static string GetObjectKeyStructure(ObjectId owner) => $"i:{owner.ObjectType.ToShortString()}:all";
    public static string GetByNameKeyStructure(GameObjectType type) => $"i:{type.ToShortString()}:names";
    public static string GetRootKey() => "i:root";
}

public class StorageManager : IStorageManager
{
    private readonly IBackendStorage _storage;

    public StorageManager(IBackendStorage storage)
    {
        _storage = storage;
    }

    public async Task<bool> AddObject(IGameObject obj)
    {
        // Set the ID if it's a brand new object
        if (obj.IsPendingAdd)
        {
            obj.SetInitialId(GeneralUtils.GenerateRandomId());
        }

        // Check if the key is in use
        var isInUse = await _storage.SetContains(StorageManagerKeys.GetObjectKeyStructure(obj.Id), obj.Id.Id);
        if (isInUse)
        {
            throw new GameObjectIdExistsException(obj.Id);
        }

        // Make sure the object has a name set
        var name = obj.Name;
        if (String.IsNullOrEmpty(name))
        {
            throw new InvalidGameObjectNameException(obj.Id);
        }

        // Make sure a player with this name doesn't already exist
        if (obj.ObjectType == GameObjectType.Player)
        {
            var exPlayer = await FindPlayerByName(name);
            if (exPlayer != null)
            {
                throw new PlayerNameAlreadyExistsException(name, new ObjectId(exPlayer));
            }
        }

        await using (var transact = _storage.StartTransaction())
        {
            UpdateHash(transact, StorageManagerKeys.GetMetaKeyStructure(obj.Id), obj.MetaBasic.ToDictionary()!);
            await transact.SetAdd(StorageManagerKeys.GetObjectKeyStructure(obj.Id), obj.Id.Id);

            if (obj.ObjectType == GameObjectType.Player)
            {
                await transact.HashSetField(StorageManagerKeys.GetByNameKeyStructure(GameObjectType.Player), name.ToLower(), obj.Id.Id);
            }

            if (obj.MetaBasic.Location != null)
            {
                var res = await ReparentMoveInTransaction(transact, "location", obj.Id, obj.MetaBasic.Location, null, false);
                if (!res)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public async Task<bool> DestroyObject(IGameObject obj)
    {
        await using (var transact = _storage.StartTransaction())
        {
            // Remove primary properties
            await transact.KeyDelete(StorageManagerKeys.GetPropKeyStructure(obj.Id));
            await transact.KeyDelete(StorageManagerKeys.GetContentsKeyStructure(obj.Id));
            await transact.KeyDelete(StorageManagerKeys.GetMetaKeyStructure(obj.Id));

            // Remove type-specific properties
            if (obj.ObjectType == GameObjectType.Player)
            {
                await transact.HashDeleteField(StorageManagerKeys.GetByNameKeyStructure(obj.ObjectType), obj.Name.ToLower());
            }
            else if (obj.ObjectType == GameObjectType.Script)
            {
                await transact.KeyDelete(StorageManagerKeys.GetScriptKeyStructure(obj.Id));
            }

            // Remove from global type list
            await transact.SetRemove(StorageManagerKeys.GetObjectKeyStructure(obj.Id), obj.Id.Id);

            // Remove from current location
            if (obj.Location != null)
            {
                await transact.SetRemove(StorageManagerKeys.GetContentsKeyStructure(obj.Location), obj.Id.Id);
            }
        }

        return true;
    }

    public Task<bool> DoesObjectExist(ObjectId id)
    {
        return _storage.SetContains(StorageManagerKeys.GetObjectKeyStructure(id), id.Id);
    }

    public Task<IReadOnlyDictionary<string, string>> GetAllPlayers()
    {
        return _storage.HashGetAll(StorageManagerKeys.GetByNameKeyStructure(GameObjectType.Player));
    }

    public Task<string?> FindPlayerByName(string name)
    {
        return _storage.HashGetField(StorageManagerKeys.GetByNameKeyStructure(GameObjectType.Player), name.ToLower());
    }

    public async Task<bool> UpdatePlayerNameIndex(ObjectId id, string oldName, string newName)
    {
        await using (var transact = _storage.StartTransaction())
        {
            await transact.HashDeleteField(StorageManagerKeys.GetByNameKeyStructure(GameObjectType.Player), oldName.ToLower());
            await transact.HashSetField(StorageManagerKeys.GetByNameKeyStructure(GameObjectType.Player), newName.ToLower(), id.Id);
        }
        return true;
    }

    public async Task<PropValue> GetProp(ObjectId owner, string path)
    {
        var prop = await _storage.HashGetField(StorageManagerKeys.GetPropKeyStructure(owner), path);
        if (prop == null)
        {
            return new PropValue();
        }

        return PropValue.FromJsonString(prop);
    }

    public async Task<IReadOnlyDictionary<string, PropValue>> GetProps(ObjectId owner)
    {
        var props = await _storage.HashGetAll(StorageManagerKeys.GetPropKeyStructure(owner));
        var deserialized = props.ToDictionary(s => s.Key, s => PropValue.FromJsonString(s.Value));
        return deserialized;
    }

    public async Task<bool> SetProp(ObjectId owner, string path, PropValue value)
    {
        var serialized = value.ToJsonString();
        await UpdateHash(StorageManagerKeys.GetPropKeyStructure(owner), path, serialized);
        return true;
    }

    public async Task<bool> SetProps(ObjectId owner, IDictionary<string, PropValue> values)
    {
        var key = StorageManagerKeys.GetPropKeyStructure(owner);
        var serialized = values.ToDictionary(s => s.Key, s => s.Value.ToJsonString()).WhereNotNull();

        await using (var transact = _storage.StartTransaction())
        {
            ReplaceHash(transact, key, serialized);
        }

        return true;
    }

    public async Task<IEnumerable<ObjectId>> GetContents(ObjectId owner, GameObjectType? type = null)
    {
        var contents = await _storage.SetMembers(StorageManagerKeys.GetContentsKeyStructure(owner));
        var contentsIds = contents.Select(s => new ObjectId(s));

        if (type == null)
        {
            return contentsIds;
        }

        return contentsIds.Where(w => w.ObjectType == type.Value);
    }

    public async Task<bool> ReparentObject(ObjectId objectId, ObjectId newParent, ObjectId? oldParent = null)
    {
        await using (var transact = _storage.StartTransaction())
        {
            return await ReparentMoveInTransaction(transact, "parent", objectId, newParent, oldParent);
        }
    }

    public async Task<bool> MoveObject(ObjectId objectId, ObjectId newLocation, ObjectId? oldLocation = null)
    {
        await using (var transact = _storage.StartTransaction())
        {
            return await ReparentMoveInTransaction(transact, "location", objectId, newLocation, oldLocation);
        }
    }

    public async Task<bool> MoveObjects(IEnumerable<ObjectId> objectIds, ObjectId newLocation, ObjectId? oldLocation = null)
    {
        await using (var transact = _storage.StartTransaction())
        {
            var ops = objectIds.Select(objectId => ReparentMoveInTransaction(transact, "location", objectId, newLocation, oldLocation));
            var res = await Task.WhenAll(ops);
            return res.All(a => !!a);
        }
    }

    public async Task<MD?> GetMeta<MD>(ObjectId objectId) where MD : ObjectMetadata
    {
        var result = await _storage.HashGetAll(StorageManagerKeys.GetMetaKeyStructure(objectId));
        if (result.Count < 1)
        {
            return null;
        }

        var meta = ObjectMetadata.FromDictionary<MD>(result);
        return meta;
    }

    public Task<ObjectMetadata?> GetMeta(ObjectId objectId)
    {
        return GetMeta<ObjectMetadata>(objectId);
    }

    public Task<string?> GetMeta(ObjectId objectId, string key)
    {
        return _storage.HashGetField(StorageManagerKeys.GetMetaKeyStructure(objectId), key);
    }

    public async Task<bool> UpdateMeta<MD>(ObjectId objectId, MD meta) where MD : ObjectMetadata
    {
        var dict = meta.ToDictionary();

        await using (var transact = _storage.StartTransaction())
        {
            ReplaceHash(transact, StorageManagerKeys.GetMetaKeyStructure(objectId), dict);
        }

        return true;
    }

    public Task<bool> UpdateMeta(ObjectId objectId, string key, string value)
    {
        return UpdateHash(StorageManagerKeys.GetMetaKeyStructure(objectId), key, value);
    }

    public Task<string?> GetRootValue(RootField field)
    {
        return _storage.HashGetField(StorageManagerKeys.GetRootKey(), RootFieldConsts.GetRootFieldString(field));
    }

    public Task<bool> SetRootValue(RootField field, string value)
    {
        return _storage.HashSetField(StorageManagerKeys.GetRootKey(), RootFieldConsts.GetRootFieldString(field), value);
    }

    public Task<string?> GetScriptCode(ObjectId objectId)
    {
        return _storage.KeyGet(StorageManagerKeys.GetScriptKeyStructure(objectId));
    }

    public Task<bool> SetScriptCode(ObjectId objectId, string code)
    {
        return _storage.KeySet(StorageManagerKeys.GetScriptKeyStructure(objectId), code);
    }

    // Helper methods

    private async Task<bool> ReparentMoveInTransaction(IBackendStorageTransaction transact, string type, ObjectId id, ObjectId newOwner, ObjectId? oldOwner = null, bool writeMeta = true)
    {
        if (newOwner == null)
        {
            return false;
        }

        if (type != "location" && type != "parent")
        {
            throw new ArgumentException("Invalid type param", "type");
        }

        // Set object's reference
        if (writeMeta)
        {
            await transact.HashSetField(StorageManagerKeys.GetMetaKeyStructure(id), type, newOwner.Id);
        }

        if (type == "location")
        {
            // Remove from old storage
            if (oldOwner != null)
            {
                await transact.SetRemove(StorageManagerKeys.GetContentsKeyStructure(oldOwner), id.Id);
            }

            // Add to new
            await transact.SetAdd(StorageManagerKeys.GetContentsKeyStructure(newOwner), id.Id);
        }

        return true;
    }

    private Task<bool> UpdateHash(string key, string field, string? value)
    {
        if (value == null)
        {
            return _storage.HashDeleteField(key, field);
        }
        else
        {
            return _storage.HashSetField(key, field, value);
        }
    }

    private void UpdateHash(IBackendStorageTransaction transact, string key, IDictionary<string, string?> values)
    {
        foreach (var val in values)
        {
            if (val.Value == null)
            {
                transact.HashDeleteField(key, val.Key);
            }
            else
            {
                transact.HashSetField(key, val.Key, val.Value);
            }
        }
    }

    private void ReplaceHash(IBackendStorageTransaction transact, string key, IDictionary<string, string> values)
    {
        transact.KeyDelete(key);

        foreach (var val in values)
        {
            transact.HashSetField(key, val.Key, val.Value);
        }
    }
}
